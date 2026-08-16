//#####################################################################################################
// PLC2PLCTagTransfer - Developed on 14 AUG 2026 by Sameer.
// Used for headless transfer of multiple tags to different PLC types (same PLC used in this example).
// The code is parameterized for multiple tags and manual placement. See instructions below.
// Change the default CommDriver name (e.g., "RAEtherNet_IPStation1") as needed.
// Note: This code uses a PeriodicTask for headless transfer, which has transfer rate limitations.
//#####################################################################################################
#region Using directives
using FTOptix.CommunicationDriver;
using FTOptix.Core;
using FTOptix.CoreBase;
using FTOptix.HMIProject;
using FTOptix.NativeUI;
using FTOptix.NetLogic;
using FTOptix.RAEtherNetIP;
using FTOptix.Retentivity;
using FTOptix.UI;
using System;
using System.Collections.Generic;
using UAManagedCore;
using OpcUa = UAManagedCore.OpcUa;
#endregion



public class PLC2PLCTagTransfer : BaseNetLogic
{
    private PeriodicTask syncTask;
    private readonly List<GenericSyncPair> syncPairs = new List<GenericSyncPair>();

    public override void Start()
    {
        // ######################################################################################
        // DEFINE YOUR TAG CONFIGURATIONS HERE (limit your tags to be transfered)
        // To add a new pair, just copy a line and change the tag names at the end!
        // Remember these tags are case sensitive and value transfer is bidirectional 
        // and if need to transfer to one direct, it needs some code modification. 
        // ######################################################################################

        // Pair 1: xTag1 (DINT) <-> pData1 (DINT)
        AddSyncPair("RAEtherNet_IPDriver1/RAEtherNet_IPStation1/Tags/Controller Tags/xTag1",
                    "RAEtherNet_IPDriver2/RAEtherNet_IPStation2/Tags/Controller Tags/pData1");

        // Pair 2: xTag2 (REAL) <-> pData2 (REAL)
        AddSyncPair("RAEtherNet_IPDriver1/RAEtherNet_IPStation1/Tags/Controller Tags/xTag2",
                    "RAEtherNet_IPDriver2/RAEtherNet_IPStation2/Tags/Controller Tags/pData2");

        // Pair 3: xTag3 (String) <-> pData3 (String)
        AddSyncPair("RAEtherNet_IPDriver1/RAEtherNet_IPStation1/Tags/Controller Tags/xTag3",
                    "RAEtherNet_IPDriver2/RAEtherNet_IPStation2/Tags/Controller Tags/pData3");
        // Pair 4: xTag4 (bool) <-> pData3 (bool)
        AddSyncPair("RAEtherNet_IPDriver1/RAEtherNet_IPStation1/Tags/Controller Tags/xTag4",
                    "RAEtherNet_IPDriver2/RAEtherNet_IPStation2/Tags/Controller Tags/pData4");
        // Pair 5: xxString222 (String) <-> xxString222 (String)
        AddSyncPair("RAEtherNet_IPDriver1/RAEtherNet_IPStation1/Tags/Controller Tags/xxString222",
                    "RAEtherNet_IPDriver2/RAEtherNet_IPStation2/Tags/Controller Tags/string222");

        // =========================================================================

        // Start the background polling task every 200ms.
        if (syncPairs.Count > 0)
        {
            syncTask = new PeriodicTask(SynchronizeHeadlessLoop, 200, LogicObject);// polling rate is 200ms, if needed you can lower it.
            syncTask.Start();
            Log.Info("PLC2PLCTagTransfer", $"SUCCESS: Headless background synchronizer is actively running {syncPairs.Count} pairs.");
        }
        else
        {
            Log.Warning("PLC2PLCTagTransfer", "No valid tag paths resolved. Synchronizer is idle.");
        }
    }

    
    private void AddSyncPair(string driver1SubPath, string driver2SubPath)
    {
        var t1 = Project.Current.GetVariable("CommDrivers/" + driver1SubPath);
        var t2 = Project.Current.GetVariable("CommDrivers/" + driver2SubPath);

        if (t1 != null && t2 != null)
        {
            
            try { t1.RemoteRead(); t2.RemoteRead(); } catch { }

            // 
            syncPairs.Add(new GenericSyncPair(t1, t2));
        }
        else
        {
            Log.Error("PLC2PLCTagTransfer", $"Path Mismatch: Could not resolve Driver1: '{driver1SubPath}' or Driver2: '{driver2SubPath}'");
        }
    }

    private void SynchronizeHeadlessLoop()
    {
        // Execute the exact same value tracking logic for every registered pair
        foreach (var pair in syncPairs)
        {
            pair.ExecuteSync();
        }
    }

    public override void Stop()
    {
        syncTask?.Dispose();
    }
}

//Tested on 08/14/2026
public class GenericSyncPair
{
    private readonly IUAVariable tag1;
    private readonly IUAVariable tag2;
    private object lastValue1;
    private object lastValue2;

    public GenericSyncPair(IUAVariable t1, IUAVariable t2)
    {
        tag1 = t1;
        tag2 = t2;
        lastValue1 = tag1.Value?.Value;
        lastValue2 = tag2.Value?.Value;
    }

    public void ExecuteSync()
    {
        try
        {
            // Query values straight from physical network connections, forcing tags to stay awake
            tag1.RemoteRead();
            tag2.RemoteRead();

            var currentVal1 = tag1.Value?.Value;
            var currentVal2 = tag2.Value?.Value;

            if (currentVal1 == null || currentVal2 == null) return;

            // Check if Driver 1 changed -> Push to Driver 2 over the network
            if (!currentVal1.Equals(lastValue1))
            {
                tag2.RemoteWrite(new UAValue(currentVal1));
                lastValue1 = currentVal1;
                lastValue2 = currentVal1;
            }
            // Check if Driver 2 changed -> Push to Driver 1 over the network
            else if (!currentVal2.Equals(lastValue2))
            {
                tag1.RemoteWrite(new UAValue(currentVal2));
                lastValue1 = currentVal2;
                lastValue2 = currentVal2;
            }
        }
        catch (Exception ex)
        {
            Log.Error("PLC2PLCTagTransfer", "Sync Pair Processing Fault: " + ex.Message);
        }
    }
}
