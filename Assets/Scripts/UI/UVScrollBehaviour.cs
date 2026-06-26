using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(RawImage))]
public class UVScrollBehaviour : MonoBehaviour
{
    public float scrollX = 0f;
    public float scrollY = 0f;
    private RawImage _img;

    void Awake() => _img = GetComponent<RawImage>();

    void Update()
    {
        var r = _img.uvRect;
        r.x += scrollX * Time.deltaTime;
        r.y += scrollY * Time.deltaTime;
        _img.uvRect = r;
    }
}
