Snapshotter
===========

Please note that this tool works for Windows volumes created from exactly *ONE* windows physical disk. STRIPED VOLUMES, THEREFORE, WILL NOT WORK HERE

# Overview

This tool enables two scenarios:

1. **Remembers all EBS attachment configurations**: In the event of an instance failure, a new instance will be able to attach to the appropriate volumes using the same EBS volumes, AWS EBS Devices (For /dev/xvdf etc) and online the volumes as drives using the same drive letter as before.

2. **Creates EBS Snapshots of non boot volumes**: In the event of failure, it simplifies creating volumes using snapshots and re-attaching the volumes to an instance while preserving the correct drive configurations.

***
