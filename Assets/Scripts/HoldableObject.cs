using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HoldableObject : MonoBehaviour
{
    public bool IsBeingHeld;
    private GameObject target;

    // Start is called before the first frame update
    void Start()
    {
        IsBeingHeld = false;
    }

    // Update is called once per frame
    void Update()
    {
        if (!IsBeingHeld || target == null) return;

        RectTransform cursorRect = target.GetComponent<RectTransform>();

        // Convert UI position ? screen position
        Vector2 screenPos = RectTransformUtility.WorldToScreenPoint(
            null,
            cursorRect.position
        );

        // Convert screen ? world (KEEP OBJECT DEPTH)
        Vector3 worldPos = Camera.main.ScreenToWorldPoint(
            new Vector3(screenPos.x, screenPos.y, Camera.main.WorldToScreenPoint(transform.position).z)
        );

        transform.position = worldPos;
    }

    public void PickUp(GameObject cursor)
    {
        IsBeingHeld = true;
        target = cursor;
    }

    public void Drop()
    {
        IsBeingHeld = false;
        target = null;
    }
}
