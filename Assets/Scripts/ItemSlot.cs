using System;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;

public class ItemSlot : MonoBehaviour, IDropHandler 
{
    public LevelManager levelManager;
    public TipoPeca tipoPeca;

    public void OnDrop(PointerEventData eventData){
        Debug.Log("Dropped");
        if (eventData.pointerDrag != null) {
            if (eventData.pointerDrag.tag == gameObject.tag) {
                switch (tipoPeca)
                {
                    case TipoPeca.Numero:
                        if (levelManager.number == eventData.pointerDrag.GetComponent<DragDrop>().Valor)
                        {
                            eventData.pointerDrag.transform.position = transform.position;
                        }
                        else
                        {
                            //Expulsa a peça daqui
                            Debug.Log("Número Incorreto");
                        }
                        break;
                    case TipoPeca.Comida:
                        Debug.Log("Slot de Comida");
                        break;

                }

            }
        }
    }
}
