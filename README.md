# OptixEdge-PLC-to-PLC-tag-transfer
OptixEdge PLC to PLC tag transfer using Netlogic C#
//###########################################################################

 PLC2PLCTagTransfer - code used for headless transfer of multiple tags to 
 
 different PLC types (same CompactLogix PLC used in this example).
 
 The code is parameterized for multiple tags and manual placement. See instructions below.
 
 Change the default CommDriver name (e.g., "RAEtherNet_IPStation1") as needed.
 
 Note: This code uses a PeriodicTask for headless transfer, which has transfer rate limitations.
 I am attaching file "PLC2PLCTagTransfer.cs" and you can copy this code and paste it on your 
 OprtixEdge -> Runtime Netlogic created ".cs" file.
//############################################################################
