using System;
using UnityEngine;
using UnityEngine.EventSystems;

public class ItemSlot : MonoBehaviour, IDropHandler 
{
    
    public void OnDrop(PointerEventData eventData){
        Debug.Log("Dropped");
        if (eventData.pointerDrag != null) {
            if (eventData.pointerDrag.tag == gameObject.tag) {
                eventData.pointerDrag.transform.position = transform.position;

            }
        }
    }
}
