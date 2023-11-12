using UnityEngine;

public class FollowDraggable : Draggable
{
    public override void OnDragStart()
    {
        base.OnDragStart();
    }
    public override void OnDragEnd()
    {
        base.OnDragEnd();
    }

    void Update()
    {
        if(!isDragging)
            return;
        var pos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        pos = new Vector3(pos.x, pos.y, -0.5f);
        transform.position = pos;
    }
}
