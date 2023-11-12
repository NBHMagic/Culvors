using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UniRx;

public class Draggable : MonoBehaviour
{
    public Subject<Unit> DragStart = new Subject<Unit>();
    public Subject<Unit> DragEnd = new Subject<Unit>();

    public bool isDragging {get;private set;} = false;
    public virtual void OnDragStart()
    {
        isDragging = true;
        DragStart.OnNext(Unit.Default);
    }
    public virtual void OnDragEnd()
    {
        isDragging = false;
        DragEnd.OnNext(Unit.Default);
    }
}
