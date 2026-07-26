#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public static class DustParticlePrefabGenerator
{
    [MenuItem("Tools/Generate Dust Particle Prefabs")]
    public static void GeneratePrefabs()
    {
        string prefabFolder = "Assets/_Project/Prefabs";
        if (!AssetDatabase.IsValidFolder(prefabFolder))
        {
            AssetDatabase.CreateFolder("Assets/_Project", "Prefabs");
        }

        // 1. Generate Jump Dust Prefab
        GameObject jumpObj = new GameObject("JumpDustParticle");
        ParticleSystem psJump = jumpObj.AddComponent<ParticleSystem>();
        SetupJumpParticleSystem(psJump);
        string jumpPath = $"{prefabFolder}/JumpDustParticle.prefab";
        PrefabUtility.SaveAsPrefabAsset(jumpObj, jumpPath);
        Object.DestroyImmediate(jumpObj);

        // 2. Generate Land Dust Prefab
        GameObject landObj = new GameObject("LandDustParticle");
        ParticleSystem psLand = landObj.AddComponent<ParticleSystem>();
        SetupLandParticleSystem(psLand);
        string landPath = $"{prefabFolder}/LandDustParticle.prefab";
        PrefabUtility.SaveAsPrefabAsset(landObj, landPath);
        Object.DestroyImmediate(landObj);

        AssetDatabase.Refresh();
        Debug.Log($"<color=green>Successfully generated Dust Particle Prefabs at {prefabFolder}!</color>");
    }

    private static Material GetParticleMaterial()
    {
        Shader particleShader = Shader.Find("Particles/Standard Unlit");
        if (particleShader == null) particleShader = Shader.Find("Sprites/Default");
        return new Material(particleShader);
    }

    private static void SetupJumpParticleSystem(ParticleSystem ps)
    {
        var main = ps.main;
        main.duration = 0.2f;
        main.loop = false;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.2f, 0.35f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(1.5f, 3.5f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.12f, 0.28f);
        main.startColor = new Color(0.9f, 0.95f, 1.0f, 0.6f);
        main.gravityModifier = 0.2f;
        main.stopAction = ParticleSystemStopAction.Destroy;

        var emission = ps.emission;
        emission.rateOverTime = 0;
        emission.SetBurst(0, new ParticleSystem.Burst(0f, 10));

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 35f;
        shape.radius = 0.15f;

        var sizeOverLifetime = ps.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1.0f, AnimationCurve.EaseInOut(0f, 1f, 1f, 0f));

        var colorOverLifetime = ps.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient grad = new Gradient();
        grad.SetKeys(
            new GradientColorKey[] { new GradientColorKey(Color.white, 0.0f), new GradientColorKey(new Color(0.85f, 0.95f, 1.0f), 1.0f) },
            new GradientAlphaKey[] { new GradientAlphaKey(0.6f, 0.0f), new GradientAlphaKey(0.0f, 1.0f) }
        );
        colorOverLifetime.color = grad;

        ParticleSystemRenderer psr = ps.GetComponent<ParticleSystemRenderer>();
        psr.material = GetParticleMaterial();
    }

    private static void SetupLandParticleSystem(ParticleSystem ps)
    {
        var main = ps.main;
        main.duration = 0.2f;
        main.loop = false;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.25f, 0.4f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(2.0f, 4.0f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.15f, 0.32f);
        main.startColor = new Color(0.9f, 0.95f, 1.0f, 0.7f);
        main.gravityModifier = 0.1f;
        main.stopAction = ParticleSystemStopAction.Destroy;

        var emission = ps.emission;
        emission.rateOverTime = 0;
        emission.SetBurst(0, new ParticleSystem.Burst(0f, 16));

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Hemisphere;
        shape.radius = 0.2f;

        var sizeOverLifetime = ps.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1.0f, AnimationCurve.EaseInOut(0f, 1f, 1f, 0.2f));

        var colorOverLifetime = ps.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient grad = new Gradient();
        grad.SetKeys(
            new GradientColorKey[] { new GradientColorKey(Color.white, 0.0f), new GradientColorKey(new Color(0.8f, 0.9f, 1.0f), 1.0f) },
            new GradientAlphaKey[] { new GradientAlphaKey(0.7f, 0.0f), new GradientAlphaKey(0.0f, 1.0f) }
        );
        colorOverLifetime.color = grad;

        ParticleSystemRenderer psr = ps.GetComponent<ParticleSystemRenderer>();
        psr.material = GetParticleMaterial();
    }
}
#endif
