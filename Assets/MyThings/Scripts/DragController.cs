using UnityEngine;

public class DragController : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    bool isDragging;
    Draggable currentDraggable;
    // Update is called once per frame
    void Update()
    {
        if(!Input.GetMouseButton(0))
        {
            EndDrag();
            return;
        }

        if(!Input.GetMouseButtonDown(0))
        {
            return;
        }

        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
		if(Physics.Raycast(ray, out var hitInfo)) 
        {
            isDragging = true;
			GameObject objectHit = hitInfo.transform.gameObject;
            var draggable = objectHit.GetComponent<Draggable>() ;
			if(draggable) 
            {
                if(currentDraggable != draggable)
                {
                    currentDraggable?.OnDragEnd();
                }
                currentDraggable = draggable;
				draggable.OnDragStart();
				
                return;
			}
		}
        EndDrag();
    }

    void EndDrag()
    {
        isDragging = false;
        if(currentDraggable)
        {
            currentDraggable.OnDragEnd();
            currentDraggable = null;
        }
    }
}
