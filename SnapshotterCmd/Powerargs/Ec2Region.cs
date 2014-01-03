using System.Collections.Generic;
using System.Linq;
using Amazon;
using PowerArgs;

namespace Cloudoman.AwsTools.SnapshotterCmd.Powerargs
{

    // <summary> 
    // Custom class used by PowerArgs to validate Ec2Regions provided as input via command line
    // </summary>
    public class Ec2Region : GroupedRegexArg
    {
        readonly string _ec2Region;

        // Get EC2 Endpoints from SDK
        static readonly IEnumerable<string> Ec2EndPoints = 
            RegionEndpoint.EnumerableAllRegions.Select(
                x => x.GetEndpointForService("ec2").Hostname
            );

        // Format into readable help message
        static readonly string HelpMessage =  
            "Invalid EC2 Region Specified. Must be:\n\t "
            + Ec2EndPoints.Aggregate((i, j) => i + "\n\t" + j);

		// Constructor
        public Ec2Region(string ec2Region) : base(Ec2RegionRegex, ec2Region,HelpMessage)
        {
            _ec2Region = ec2Region;
        }

        [ArgReviver]
        public static Ec2Region Revive(string key, string val)
        {
            return new Ec2Region(val);
        }


		// Provides regex to validate region endpoints
        public static string Ec2RegionRegex
        {
            get
            {
                // Return endpoints delimiting with OR operator "|"
                return Ec2EndPoints.Aggregate((i, j) => i + "|" + j);
            }
        }

		// Returns string representation for EC2 Region
        public override string ToString()
        {
            return _ec2Region;
        }
    }
}