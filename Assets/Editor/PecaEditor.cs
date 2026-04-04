using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(DragDrop))]
public class DragDropEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        DragDrop peca = (DragDrop)target;

        if (peca.tipoPeca == TipoPeca.Numero)
            peca.valorNumero = (ValorNumero)EditorGUILayout.EnumPopup("Valor", peca.valorNumero);
        else
            peca.valorComida = (ValorComida)EditorGUILayout.EnumPopup("Valor", peca.valorComida);
    }
}