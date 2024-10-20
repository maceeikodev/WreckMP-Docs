using MSCLoader;
using System.Collections.Generic;
using UnityEngine;

namespace WreckMPExampleMod;

public class WreckMPExampleMod : Mod
{
    public override string ID => "WreckMPExampleMod";
    public override string Name => "WreckMP example mod";
    public override string Author => "Honeycomb936 and Maceeiko";
    public override string Version => "1.0";
    public override string Description => "Short demonstration on how to sync a mod for WreckMP multiplayer";

    static bool MPPresent;

    List<MeshRenderer> cube;
    Transform player;

    object colorEvent, spawnCubeEvent, initialSyncEvent;

    public override void ModSetup() 
    {
        SetupFunction(Setup.OnLoad, Mod_OnLoad);
        SetupFunction(Setup.Update, Mod_Update);
    }

    void Mod_OnLoad()
    {
        MPPresent = System.Environment.GetEnvironmentVariable("WreckMP-Present") != null;

        cube = new List<MeshRenderer>();
        player = GameObject.Find("PLAYER").transform;

        if (MPPresent) InitMP();
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
        // Put mesh in child so it's jumpable by player
        var cube = new GameObject("cube(xxxxx)");
        cube.transform.position = position;
        var rb = cube.AddComponent<Rigidbody>();
        rb.mass = 10;
        var col = cube.AddComponent<BoxCollider>();
        col.size = Vector3.one * 0.5f;
        col.isTrigger = true;
        cube.MakePickable();

        var mesh = GameObject.CreatePrimitive(PrimitiveType.Cube);
        mesh.transform.localScale = Vector3.one * 0.5f;
        mesh.transform.parent = cube.transform;
        mesh.transform.localPosition = mesh.transform.localEulerAngles = Vector3.zero;
        var mr = mesh.GetComponent<MeshRenderer>();
        this.cube.Add(mr);

        if (MPPresent)
        {
            if (sendEvent) SendSpawnCube(position);
            RegisterCube(rb, this.cube.Count);
        }
        return mr;
    }

    void RandomizeCubeColor(int index)
    {
        var col = new Color(Random.Range(0, 1f), Random.Range(0, 1f), Random.Range(0, 1f), 1f);
        cube[index].material.color = col;

        if (MPPresent) SendColorUpdate(index, col.r, col.g, col.b);
    }

    // MP METHODS
    void InitMP()
    {
        colorEvent = new WreckMP.GameEvent("example_colorUpdate", ReceiveColorUpdate);
        spawnCubeEvent = new WreckMP.GameEvent("example_spawnCube", ReceiveSpawnCube);
        initialSyncEvent = new WreckMP.GameEvent("example_initSync", ReceiveInitialSync);

        if (WreckMP.WreckMPGlobals.IsHost)
        {
            WreckMP.WreckMPGlobals.OnMemberReady.Add(SendInitialSync);
        }
    }

    void SendSpawnCube(Vector3 cubePosition)
    {
        var e = spawnCubeEvent as WreckMP.GameEvent;
        using var p = e.Writer();
        p.Write(cubePosition);
        e.Send(p);
    }

    private void ReceiveSpawnCube(WreckMP.GameEventReader obj)
    {
        var cubePosition = obj.ReadVector3();
        MakeCube(cubePosition, false);
    }

    void SendInitialSync(ulong user)
    {
        var e = initialSyncEvent as WreckMP.GameEvent;
        using var p = e.Writer();
        p.Write(cube.Count);
        for (int i = 0; i < cube.Count; i++)
        {
            p.Write(cube[i].transform.position);
            var col = cube[i].material.color;
            p.Write(col.r);
            p.Write(col.g);
            p.Write(col.b);
        }
        e.Send(p, user);
    }

    private void ReceiveInitialSync(WreckMP.GameEventReader obj)
    {
        var count = obj.ReadInt32();
        for (int i = 0; i < count; i++)
        {
            var cube = MakeCube(obj.ReadVector3(), false);
            cube.material.color = new Color
            {
                r = obj.ReadSingle(),
                g = obj.ReadSingle(),
                b = obj.ReadSingle()
            };
        }
    }

    void SendColorUpdate(int cubeIndex, float r, float g, float b) 
    {
        var e = colorEvent as WreckMP.GameEvent;
        using var writer = e.Writer();
        writer.Write(cubeIndex);
        writer.Write(r);
        writer.Write(g);
        writer.Write(b);
        e.Send(writer);
    }

    void ReceiveColorUpdate(WreckMP.GameEventReader packet)
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

    void RegisterCube(Rigidbody rb, int i)
    {
        WreckMP.NetRigidbodyManager.AddRigidbody(rb, $"grabbable cube index {i}".GetHashCode());
    }
}
