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

        if (WreckMPGlobals.IsHost)
        {
            WreckMPGlobals.OnMemberReady(SendInitialSync);
        }
    }

    void Mod_Update()
    {
        if (Input.GetKeyUp(KeyCode.End)) MakeCube(player.position, true);
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
        rb.RegisterRigidbody($"grabbable cube index {this.cube.Count}".GetHashCode());

        var col = cube.AddComponent<BoxCollider>();
        col.size = Vector3.one * 0.5f;
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
            p.Write(position);
            p.Send();
        }

        return mr;
    }

    void ReceiveSpawnCube(GameEventReader packet)
    {
        var cubePosition = packet.ReadVector3();
        MakeCube(cubePosition, false);
    }

    void RandomizeCubeColor(int index)
    {
        var col = new Color(Random.Range(0, 1f), Random.Range(0, 1f), Random.Range(0, 1f), 1f);
        cube[index].material.color = col;

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
        cube[cubeIndex].material.color = color;
    }

    void SendInitialSync(ulong user)
    {
        using var p = initialSyncEvent.Writer();
        p.Write(cube.Count);
        for (int i = 0; i < cube.Count; i++)
        {
            p.Write(cube[i].transform.position);
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
            cube.material.color = new Color
            {
                r = packet.ReadSingle(),
                g = packet.ReadSingle(),
                b = packet.ReadSingle()
            };
        }
    }
}
