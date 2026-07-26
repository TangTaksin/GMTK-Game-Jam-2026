using UnityEngine;

public static class DustParticleEffects
{
    private static Material defaultParticleMaterial;

    private static Material GetParticleMaterial()
    {
        if (defaultParticleMaterial == null)
        {
#if UNITY_EDITOR
            defaultParticleMaterial = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Art/Materials/PixelDustMat.mat");
#endif
            if (defaultParticleMaterial == null)
            {
                Shader particleShader = Shader.Find("Particles/Standard Unlit");
                if (particleShader == null) particleShader = Shader.Find("Sprites/Default");
                defaultParticleMaterial = new Material(particleShader);
            }
        }
        return defaultParticleMaterial;
    }

    public static void PlayJumpDust(Vector3 position)
    {
        GameObject pObj = new GameObject("JumpDustParticle");
        pObj.transform.position = position;

        ParticleSystem ps = pObj.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        var main = ps.main;
        main.playOnAwake = false;
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

        ParticleSystemRenderer psr = pObj.GetComponent<ParticleSystemRenderer>();
        psr.material = GetParticleMaterial();
        psr.sortingOrder = 10;

        ps.Play();
    }

    public static void PlayLandDust(Vector3 position)
    {
        GameObject pObj = new GameObject("LandDustParticle");
        pObj.transform.position = position;

        ParticleSystem ps = pObj.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        var main = ps.main;
        main.playOnAwake = false;
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

        ParticleSystemRenderer psr = pObj.GetComponent<ParticleSystemRenderer>();
        psr.material = GetParticleMaterial();
        psr.sortingOrder = 10;

        ps.Play();
    }

    public static void PlayExplosion(Vector3 position)
    {
        GameObject expObj = new GameObject("ExplosionParticleRuntime");
        expObj.transform.position = position;

        ParticleSystem ps = expObj.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        var main = ps.main;
        main.playOnAwake = false;
        main.duration = 0.6f;
        main.loop = false;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.4f, 0.7f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(6.0f, 14.0f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.7f, 1.4f);
        main.startColor = new Color(1.0f, 0.85f, 0.2f, 1.0f);
        main.gravityModifier = 0.15f;
        main.stopAction = ParticleSystemStopAction.Destroy;

        var em = ps.emission;
        em.rateOverTime = 0;
        em.SetBurst(0, new ParticleSystem.Burst(0f, 40));

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.4f;

        var colorOverLifetime = ps.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient grad = new Gradient();
        grad.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(new Color(1f, 0.95f, 0.3f), 0.0f),
                new GradientColorKey(new Color(1f, 0.45f, 0.0f), 0.35f),
                new GradientColorKey(new Color(0.85f, 0.1f, 0.0f), 0.7f),
                new GradientColorKey(new Color(0.2f, 0.2f, 0.2f), 1.0f)
            },
            new GradientAlphaKey[] {
                new GradientAlphaKey(1.0f, 0.0f),
                new GradientAlphaKey(1.0f, 0.6f),
                new GradientAlphaKey(0.0f, 1.0f)
            }
        );
        colorOverLifetime.color = grad;

        ParticleSystemRenderer psr = expObj.GetComponent<ParticleSystemRenderer>();
        psr.material = GetParticleMaterial();
        psr.sortingOrder = 20;

        ps.Play(true);
    }

    public static void PlayBloodSplatter(Vector3 position)
    {
        GameObject bObj = new GameObject("BloodSplatterRuntime");
        bObj.transform.position = position;

        ParticleSystem ps = bObj.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        var main = ps.main;
        main.playOnAwake = false;
        main.duration = 0.5f;
        main.loop = false;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.3f, 0.7f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(3.5f, 9.0f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.25f, 0.55f);
        main.startColor = new Color(0.9f, 0.05f, 0.05f, 1.0f);
        main.gravityModifier = 1.8f;
        main.stopAction = ParticleSystemStopAction.Destroy;

        var em = ps.emission;
        em.rateOverTime = 0;
        em.SetBurst(0, new ParticleSystem.Burst(0f, 30));

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 45f;
        shape.radius = 0.15f;

        var colorOverLifetime = ps.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient grad = new Gradient();
        grad.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(new Color(0.95f, 0.1f, 0.1f), 0.0f),
                new GradientColorKey(new Color(0.6f, 0.02f, 0.02f), 0.6f),
                new GradientColorKey(new Color(0.3f, 0.0f, 0.0f), 1.0f)
            },
            new GradientAlphaKey[] {
                new GradientAlphaKey(1.0f, 0.0f),
                new GradientAlphaKey(1.0f, 0.7f),
                new GradientAlphaKey(0.0f, 1.0f)
            }
        );
        colorOverLifetime.color = grad;

        var sizeOverLifetime = ps.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1.0f, AnimationCurve.EaseInOut(0f, 1f, 1f, 0.3f));

        ParticleSystemRenderer psr = bObj.GetComponent<ParticleSystemRenderer>();
#if UNITY_EDITOR
        Material bloodMat = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Art/Materials/PixelBloodMat.mat");
        if (bloodMat != null) psr.material = bloodMat;
        else psr.material = GetParticleMaterial();
#else
        psr.material = GetParticleMaterial();
#endif
        psr.sortingOrder = 25;

        ps.Play();
    }
}
