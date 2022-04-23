using Anura.ConfigurationModule.Managers;
using Anura.Templates.MonoSingleton;
using UnityEngine;

public class VFXManager : MonoSingleton<VFXManager>
{
    public void InstantiateExplosion(Vector3 position)
    {
        var explosion = ConfigurationManager.Instance.VFXConfig.GetExplosion();
        Instantiate(explosion, position, Quaternion.identity);
    }
}
