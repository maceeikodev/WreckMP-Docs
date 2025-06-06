using MSCLoader;
using System.Collections.Generic;
using UnityEngine;
using WreckAPI;

namespace WreckMPExampleMod;

public class WreckMPExampleMod : Mod
{
    public override string ID => "WreckMPExampleMod";
    public override string Name => "WreckMP example mod";
    public override string Author => "Honeycomb936 and Maceeiko";
    public override string Version => "1.0.2";
    public override string Description => "Short demonstration on how to sync a mod for WreckMP multiplayer";

    // List of all spawned cubes
    List<MeshRenderer> cube;
    Transform player;

    GameEvent colorEvent, spawnCubeEvent, initialSyncEvent;

    public override void ModSetup() 
    {
        SetupFunction(Setup.OnMenuLoad, () =>
        {
            ModConsole.Log($"WreckMP example mod ready for {(WreckMPGlobals.IsMultiplayerSession ? "multi" : "single")}player!");
        });
        SetupFunction(Setup.OnLoad, Mod_OnLoad);
        SetupFunction(Setup.Update, Mod_Update);
    }

    void Mod_OnLoad()
    {
        cube = new List<MeshRenderer>();
        player = GameObject.Find("PLAYER").transform;

        colorEvent = new GameEvent("example_colorUpdate", ReceiveColorUpdate);
        spawnCubeEvent = new GameEvent("example_spawnCube", ReceiveSpawnCube);
        initialSyncEvent = new GameEvent("example_initSync", ReceiveInitialSync);

        // Host takes care of initial sync
        if (WreckMPGlobals.IsHost)
        {
            WreckMPGlobals.OnMemberReady(SendInitialSync);
        }
    }

    void Mod_Update()
    {
        // Spawn new cube
        if (Input.GetKeyUp(KeyCode.End)) MakeCube(player.position, true);
        // Randomize color of nearest cube
        if (Input.GetKeyDown(KeyCode.Home) && cube.Count > 0)
        {
            int nearest = 0;
            float nearestDist = float.MaxValue;
            for (int i = 0; i < cube.Count; i++)
            {
                var dist = (cube[i].transform.position - player.position).sqrMagnitude;
                if (dist < nearestDist)
                {
                    nearest = i;
                    nearestDist = dist;
                }
            }
            RandomizeCubeColor(nearest);
        }
    }

    MeshRenderer MakeCube(Vector3 position, bool sendEvent)
    {
        var cube = new GameObject("cube(xxxxx)");
        cube.transform.position = position;

        var rb = cube.AddComponent<Rigidbody>();
        rb.mass = 10;
        // Register rigidbody because it's created after the game loaded
        rb.RegisterRigidbody($"grabbable cube index {this.cube.Count}".GetHashCode());

        var col = cube.AddComponent<BoxCollider>();
        col.size = Vector3.one * 0.5f;
        // Collider only for raycast
        col.isTrigger = true;

        cube.MakePickable();

        // Put mesh in child so it's jumpable by player
        var mesh = GameObject.CreatePrimitive(PrimitiveType.Cube);
        mesh.transform.localScale = Vector3.one * 0.5f;
        mesh.transform.parent = cube.transform;
        mesh.transform.localPosition = mesh.transform.localEulerAngles = Vector3.zero;
        var mr = mesh.GetComponent<MeshRenderer>();
        this.cube.Add(mr);

        if (sendEvent)
        {
            using var p = spawnCubeEvent.Writer();
            // Only spawn position of the cube is required to send,
            // as it's the only thing that will differ every event
            p.Write(position);
            p.Send();
        }

        return mr;
    }

    void ReceiveSpawnCube(GameEventReader packet)
    {
        var cubePosition = packet.ReadVector3();
        // Make sure to not send event from its own callback
        // otherwise you'd create a loop!
        // With more than 2 players you'd initiate packet
        // duplication which is an unstoppable death
        MakeCube(cubePosition, false);
    }

    void RandomizeCubeColor(int index)
    {
        var col = new Color(Random.Range(0, 1f), Random.Range(0, 1f), Random.Range(0, 1f), 1f);
        cube[index].material.color = col;

        // Write index in order to identify the cube,
        // and only relevant color channels R,G,B
        // sending ALPHA channel is useless
        using var writer = colorEvent.Writer();
        writer.Write(index);
        writer.Write(col.r);
        writer.Write(col.g);
        writer.Write(col.b);
        writer.Send();
    }

    void ReceiveColorUpdate(GameEventReader packet)
    {
        int cubeIndex = packet.ReadInt32();
        var color = new Color
        {
            r = packet.ReadSingle(),
            g = packet.ReadSingle(),
            b = packet.ReadSingle()
        };
        // No need to check the index as the cube list
        // is synced via the cube spawn events
        cube[cubeIndex].material.color = color;
    }

    void SendInitialSync(ulong user)
    {
        // When a user joins, he needs to know
        // all cubes that exist, in order to sync up properly
        using var p = initialSyncEvent.Writer();
        // Since we're writing variable count of
        // cubes, start with writing the length so we know
        // how many times to loop when reading
        p.Write(cube.Count);
        for (int i = 0; i < cube.Count; i++)
        {
            // Write only important info; transform and color
            p.Write(cube[i].transform.position);
            // Write euler angles instead of quaternion as it's smaller
            p.Write(cube[i].transform.eulerAngles);
            var col = cube[i].material.color;
            p.Write(col.r);
            p.Write(col.g);
            p.Write(col.b);
        }
        p.Send(user);
    }

    void ReceiveInitialSync(GameEventReader packet)
    {
        var count = packet.ReadInt32();
        for (int i = 0; i < count; i++)
        {
            var cube = MakeCube(packet.ReadVector3(), false);
            cube.transform.eulerAngles = packet.ReadVector3();
            cube.material.color = new Color
            {
                r = packet.ReadSingle(),
                g = packet.ReadSingle(),
                b = packet.ReadSingle()
            };
        }
    }
}
