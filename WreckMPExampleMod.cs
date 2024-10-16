using MSCLoader;
using System;
using UnityEngine;

namespace WreckMPExampleMod;

public class WreckMPExampleMod : Mod
{
    public override string ID => "WreckMPExampleMod";
    public override string Name => "WreckMP example mod";
    public override string Author => "Honeycomb936";
    public override string Version => "1.0";
    public override string Description => "Short demonstration on how to sync a mod for WreckMP multiplayer";

    bool MPPresent;

    Color col;
    GameObject cube;

    WreckMP.GameEvent colorEvent;

    public override void ModSetup() 
    {
        SetupFunction(Setup.OnLoad, Mod_OnLoad);
        SetupFunction(Setup.Update, Mod_Update);
    }

    void Mod_OnLoad()
    {
        MPPresent = Environment.GetEnvironmentVariable("WreckMP-Present") != null;

        cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.name = "cube(12345)";
        cube.AddComponent<Rigidbody>().mass = 10;
        col = cube.GetComponent<MeshRenderer>().material.color;
        cube.MakePickable();

        if (MPPresent) InitMP();
    }

    void Mod_Update()
    {
        if (Input.GetKeyDown(KeyCode.Home)) RandomizeCubeColor(); 
    }

    void RandomizeCubeColor()
    {
        col.r = UnityEngine.Random.Range(0, 1f);
        col.g = UnityEngine.Random.Range(0, 1f);
        col.b = UnityEngine.Random.Range(0, 1f);

        cube.GetComponent<MeshRenderer>().material.color = col;

        if (MPPresent) SendMP(col.r, col.g, col.b);
    }

    // MP METHODS
    void InitMP() => colorEvent = new WreckMP.GameEvent("example_colorUpdate", ReceiveMP);

    void SendMP(float r, float g, float b) 
    {
        using WreckMP.GameEventWriter writer = colorEvent.Writer();
        writer.Write(r);
        writer.Write(g);
        writer.Write(b);
        colorEvent.Send(writer);
    }

    void ReceiveMP(WreckMP.GameEventReader packet)
    {
        col.r = packet.ReadSingle();
        col.g = packet.ReadSingle();
        col.b = packet.ReadSingle();

        cube.GetComponent<MeshRenderer>().material.color = col;
    }
}
