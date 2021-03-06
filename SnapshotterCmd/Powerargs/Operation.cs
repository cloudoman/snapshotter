﻿using System;
using System.Collections.Generic;
using System.Linq;
using Amazon.RDS.Model;
using PowerArgs;

namespace Cloudoman.AwsTools.SnapshotterCmd.Powerargs
{

    // <summary> 
    // Custom class used by PowerArgs to validate Ec2Regions provided as input via command line
    // </summary>
    public class Operation : GroupedRegexArg
    {
        readonly string _operation;

        // List valid options
        static readonly IEnumerable<string> Options = new List<string> {
            "snapshotvolumes", 
            "restoresnapshots", 
            "tagvolumes", 
            "restorevolumes", 
            "listvolumes", 
            "listsnapshots"
        };

        // Format into readable help message
        private static readonly string HelpMessage = "Invalid, Operation Specified. Must be:" +
                                            String.Join(",", Options.ToArray());

        // Constructor
        public Operation(string operation)
            : base(OperationRegex, operation, HelpMessage)
        {
            _operation = operation;
        }

        [ArgReviver]
        public static Operation Revive(string key, string val)
        {
            return new Operation(val);
        }


		// Provides regex to validate region endpoints
        public static string OperationRegex
        {
            get
            {
                // Return endpoints delimiting with OR operator "|"
                return Options.Aggregate((i, j) => i + "|" + j);
            }
        }

		// Returns string representation for EC2 Region
        public override string ToString()
        {
            return _operation;
        }
    }
}