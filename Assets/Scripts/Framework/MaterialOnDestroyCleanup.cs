using UnityEngine;

/// <summary>
/// Attach to any GameObject that owns a runtime-created Material (via <c>new Material(...)</c>)
/// to ensure the material is destroyed when the GameObject is destroyed.
///
/// Unity only auto-destroys material instances it created internally (e.g., via the
/// <c>renderer.material</c> accessor). Materials created with <c>new Material()</c> and
/// assigned to <c>renderer.sharedMaterial</c> or <c>renderer.material</c> directly are
/// the caller's responsibility to destroy.
///
/// Usage:
/// <code>
/// var mat = new Material(shader);
/// renderer.material = mat;
/// go.AddComponent&lt;MaterialOnDestroyCleanup&gt;().Track(mat);
/// </code>
/// </summary>
public sealed class MaterialOnDestroyCleanup : MonoBehaviour
{
    // Up to 4 materials per component; add overloads or a list if more are needed.
    private Material _mat0;
    private Material _mat1;
    private Material _mat2;
    private Material _mat3;

    /// <summary>Register a material to be destroyed when this component's GameObject is destroyed.</summary>
    public MaterialOnDestroyCleanup Track(Material mat)
    {
        if (_mat0 == null)      { _mat0 = mat; }
        else if (_mat1 == null) { _mat1 = mat; }
        else if (_mat2 == null) { _mat2 = mat; }
        else if (_mat3 == null) { _mat3 = mat; }
        else
        {
            Debug.LogWarning("[MaterialOnDestroyCleanup] More than 4 materials tracked on one component — " +
                             "add an additional component or extend to a List<Material>.");
        }
        return this;
    }

    private void OnDestroy()
    {
        if (_mat0 != null) { Destroy(_mat0); _mat0 = null; }
        if (_mat1 != null) { Destroy(_mat1); _mat1 = null; }
        if (_mat2 != null) { Destroy(_mat2); _mat2 = null; }
        if (_mat3 != null) { Destroy(_mat3); _mat3 = null; }
    }
}
