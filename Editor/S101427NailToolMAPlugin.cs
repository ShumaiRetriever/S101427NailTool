using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using nadena.dev.ndmf;
using nadena.dev.modular_avatar.core;

[assembly: ExportsPlugin(typeof(S101427NailToolMAPlugin))]

public class S101427NailToolMAPlugin : Plugin<S101427NailToolMAPlugin>
{
    protected override void Configure()
    {
        InPhase(BuildPhase.Resolving)
            .BeforePlugin("nadena.dev.modular-avatar")
            .Run("Apply NailPositionCorrector Datas", ctx =>
            {
                Debug.Log("Apply NailPositionCorrector Datas");
                foreach (var corrector in ctx.AvatarRootObject.GetComponentsInChildren<NailPositionCorrector>())
                {
                    // corrector.Apply();
                    // Object.DestroyImmediate(corrector); // for Debug
                }
            });
    }
}