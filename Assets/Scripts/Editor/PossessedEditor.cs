using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(Possessed))]
public class PossessedEditor : Editor
{
    SerializedProperty actionTypeProp;
    SerializedProperty objectToActivateProp;
    SerializedProperty sceneToLoadProp;
    SerializedProperty delayBeforeActionProp;
    SerializedProperty onCollisionEventProp;

    private void OnEnable()
    {
        // Bind the SerializedProperties
        actionTypeProp = serializedObject.FindProperty("actionType");
        objectToActivateProp = serializedObject.FindProperty("objectToActivate");
        sceneToLoadProp = serializedObject.FindProperty("sceneToLoad");
        delayBeforeActionProp = serializedObject.FindProperty("delayBeforeAction");
        onCollisionEventProp = serializedObject.FindProperty("onCollisionEvent");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.PropertyField(actionTypeProp);

        Possessed.ActionType actionType = (Possessed.ActionType)actionTypeProp.enumValueIndex;

        // Show or hide properties based on the selected ActionType
        switch (actionType)
        {
            case Possessed.ActionType.ActivateGameObject:
                EditorGUILayout.PropertyField(objectToActivateProp);
                break;
            case Possessed.ActionType.LoadScene:
                EditorGUILayout.PropertyField(sceneToLoadProp);
                break;
        }

        EditorGUILayout.PropertyField(delayBeforeActionProp);
        EditorGUILayout.PropertyField(onCollisionEventProp);

        serializedObject.ApplyModifiedProperties();
    }
}