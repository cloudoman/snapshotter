Snapshotter
===========

AWS EBS Snapshot Backup Tool for Windows. Please not that this tool is designed to run on the EC2 instance whose volumes need to be snapshotted.

The tool provides two primary backup like capabilities:

1. Create snapshots of all non-boot EBS volumes and restore them.
2. Remember all EBS volume attachments to an EC2 instance. In case of instance failure, this tools provides the capability to automatically attached to the previously attached EBS volumes.




