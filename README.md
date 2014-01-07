Snapshotter
===========

AWS EBS Snapshot Backup Tool for Windows. Please note that this tool is designed to run on the EC2 instance whose volume(s) need to be snapshotted. As a best practice, please ensure your instances are launched with an IAM instance profile. The temporary IAM credentials assigned to the instance will be used for all AWS API calls.


The tool provides two primary backup like capabilities:

1. Create snapshots of all non-boot EBS volumes and restore them.
2. Remember all EBS volume attachments to an EC2 instance. In case of instance failure, this tools provides the capability to automatically attached to the previously attached EBS volumes.




Please refer to the wiki(https://github.com/cloudoman/snapshotter/wiki) for moe details.
