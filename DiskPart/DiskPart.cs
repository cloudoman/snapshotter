using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Cloudoman.DiskTools.Models;

namespace Cloudoman.DiskTools
{
    public class DiskPart
    {
        readonly ProcessStartInfo _psInfo;

        public DiskPart()
        {
            _psInfo = new ProcessStartInfo{
                FileName = @"c:\windows\System32\WindowsPowerShell\v1.0\powershell.exe",
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = false,
                WindowStyle = ProcessWindowStyle.Hidden
            };
        }

        /// <summary>
        /// Runs a diskpart command
        /// </summary>
        /// <param name="command">diskpart command. For e.g. list disks</param>
        /// <returns></returns>
        public string[] RunCommand(string command)
        {
            // Genereate powershell command
            // To pipe commands to diskpart
            var cmd = "\"Write-Output \"\"" + command + "\"\"\" | diskpart";
            var arguments = String.Format(" -command {0}", cmd);
            _psInfo.Arguments = arguments;

            // Get ouput from diskpart
            var process = Process.Start(_psInfo);
            if (process == null) return null;
            process.WaitForExit();

            var rawOutput =
                process.StandardOutput.ReadToEnd()
                    .Split(new[] { "\n", "\r\n" }, StringSplitOptions.RemoveEmptyEntries);

            // Return output
            return rawOutput;
        }

        public IEnumerable<Volume> ListVolume()
        {
            var rawOutput = RunCommand("list volume");
            var count = rawOutput.Count();

            return rawOutput
                       .Skip(5) // Skip lines from top
                       .Take(count - 5 - 1) //Take All except last line
                       .Where(x => !x.Contains("---")) // Ignore header formatting
                       .Select(x => new Volume
                       {
                           Num = int.Parse(x.Substring(2,10).Split(' ')[1]),
                           Letter = x.Substring(14,3).Trim().NullIfEmpty(),
                           Label = x.Substring(19, 11).Trim().NullIfEmpty(),
                           FileSystem = x.Substring(32, 4).Trim().NullIfEmpty(),
                           Type = x.Substring(39, 10).Trim().NullIfEmpty(),
                           Size = x.Substring(51, 7).Trim().NullIfEmpty(),
                           Status = x.Substring(60, 9).Trim().NullIfEmpty(),
                           Info = x.Substring(69, 8).Trim().NullIfEmpty()
                       });
        }

        public IEnumerable<Disk> ListDisk()
        {
            var rawOutput = RunCommand("list disk");
            var count = rawOutput.Count();

            return rawOutput
                       .Skip(5) // Skip lines from top
                       .Take(count - 5 - 1) //Take All except last line
                       .Where(x => !x.Contains("---")) // Ignore line header formatting
                       .Select(x => new Disk
                       {
                           Num = int.Parse(x.Substring(2, 8).Split(' ')[1]),
                           Status = x.Substring(12,13).Trim().NullIfEmpty(),
                           Size = x.Substring(27,7).Replace(" ","").Trim().NullIfEmpty(),
                           Free = x.Substring(36,7).Replace(" ","").Trim().NullIfEmpty(),
                           Dyn = x.Substring(45,3).Trim().NullIfEmpty(),
                           Gpt = x.Substring(50,2).Trim().NullIfEmpty()
                       });
        }

        public DiskPartResponse OnlineDisk(int diskNumber)
        {
            var status = false;
            var command = @"
                select disk $diskNumber
                online disk NOERR
                attributes disk clear readonly NOERR
                EXIT
            ";
            command = command.Replace("$diskNumber", diskNumber.ToString());

            var output = RunCommand(command);

            var message = "successfully onlined the selected disk";
            var message2 = "This disk is already online";
            status = (output.ToList().Any(x => x.ToLower().Contains(message.ToLower()) || x.ToLower().Contains(message2.ToLower())));

            return new DiskPartResponse
            {
                Status = status,
                Output = output
            };
        }

        public DiskPartResponse OfflineDisk(int diskNumber)
        {
            var status = false;
            var command = @"
                select disk $diskNumber
                offline disk NOERR
                EXIT
            ";
            command = command.Replace("$diskNumber", diskNumber.ToString());

            var output = RunCommand(command);

            var message = "successfully offlined the selected disk";
            var message2 = "This disk is already offline";
            status = (output.ToList().Any(x => x.ToLower().Contains(message.ToLower()) || x.ToLower().Contains(message2.ToLower())));

            return new DiskPartResponse
            {
                Status = status,
                Output = output
            };
        }
        public VolumeDetail VolumeDetail(int volumeNumber)
        {
            // Run Disk Part Comand to get Volume Detail
            var command = @"
                select volume $volumeNumber
                detail volume
                EXIT
            ";
            command = command.Replace("$volumeNumber", volumeNumber.ToString());

            var output = RunCommand(command);

            // Check if volume has attached disks
            const string error = "There are no disks attached to this volume";
            var noDisk = output.ToList().Any(x => x.ToLower().Contains(error.ToLower()));

            // Extract volume details from diskpart output
            var volumeDetail = new VolumeDetail
            {
                ReadOnly = output.GetBool("Read-only"),
                Hidden = output.GetBool("Hidden"),
                NoDefaultDriveLetter = output.GetBool("No Default Drive Letter"),
                ShadowCopy = output.GetBool("Shadow Copy"),
                Offline = output.GetBool("Offline"),
                BitLockerEncrypted = output.GetBool("BitLocker Encrypted"),
                Installable = output.GetBool("Installable"),
            };

            if (noDisk) return volumeDetail;

            // Extract Disk details from diskpart output if it exists
            var disk = output.Skip(9).Take(1).Select(x => new Disk
            {
                Num = int.Parse(x.Substring(2, 8).Split(' ')[1]),
                Status = x.Substring(12, 13).Trim().NullIfEmpty(),
                Size = x.Substring(27, 7).Replace(" ", "").Trim().NullIfEmpty(),
                Free = x.Substring(36, 7).Replace(" ", "").Trim().NullIfEmpty(),
                Dyn = x.Substring(45, 3).Trim().NullIfEmpty(),
                Gpt = x.Substring(50, 2).Trim().NullIfEmpty()
            }).FirstOrDefault();

            // Get additional Volume detail when disks are attached
            volumeDetail.Disk = disk;
            volumeDetail.VolumeCapacity = output.GetString("Volume Capacity");
            volumeDetail.VolumeFreeSpace = output.GetString("Volume Free Space");

            return volumeDetail;
        }

        public DiskDetail DiskDetail(int diskNumber)
        {
            var disks = ListDisk();
            
            // Return if disk does not exist
            if (disks.Count(x => x.Num == diskNumber) == 0 ) return null;
            
            // Run Disk Part Comand to get Volume Detail
            var command = @"
                select disk $number
                detail disk
                EXIT
            ";
            command = command.Replace("$number", diskNumber.ToString());

            var output = RunCommand(command);

            var diskDetail = new DiskDetail
            {
                DiskId = output.GetString("Disk ID"),
                Type = output.GetString("Type"),
                Status = output.GetString("Status"),
                Path = output.GetString("Path"),
                Target = output.GetString("Target"),
                LunId = output.GetString("LUN ID"),
                LocationPath = output.GetString("Location Path"),
                ReadOnly = output.GetBool("Current Read-only State"),
                BootDisk = output.GetBool("Boot Disk"),
                PageFileDisk = output.GetBool("Pagefile Disk"),
                HibernationFileDisk = output.GetBool("Hibernation File Disk"),
                CrashdumpDisk = output.GetBool("Crashdump Disk"),
                ClusteredDisk = output.GetBool("Clustered Disk")
            };

            if (!diskDetail.Status.ToLower().Contains("offline"))
            {
                diskDetail.Volume = output.Skip(24).Take(1)
                                         .Select(x => new Volume
                                         {
                                             Num = int.Parse(x.Substring(2, 10).Split(' ')[1]),
                                             Letter = x.Substring(14, 3).Trim().NullIfEmpty(),
                                             Label = x.Substring(19, 11).Trim().NullIfEmpty(),
                                             FileSystem = x.Substring(32, 4).Trim().NullIfEmpty(),
                                             Type = x.Substring(39, 10).Trim().NullIfEmpty(),
                                             Size = x.Substring(51, 7).Trim().NullIfEmpty(),
                                             Status = x.Substring(60, 9).Trim().NullIfEmpty(),
                                             Info = x.Substring(69, 8).Trim().NullIfEmpty()
                                         }).FirstOrDefault();
            }
            return diskDetail;

        }
        public DiskPartResponse AssignDriveLetter(int volumeNumber, string letter)
        {
            var status = false;
            var command = @"
                select volume $volumeNumber
                assign letter $letter NOERR
                EXIT
            ";

            command = command.Replace("$volumeNumber", volumeNumber.ToString());
            command = command.Replace("$letter", letter.ToUpper());

            var output = RunCommand(command);

            var message = "successfully assigned the drive letter";
            status = (output.ToList().Any(x => x.Contains(message)));

            return new DiskPartResponse
            {
                Status = status,
                Output = output
            };
        }


    }
}
