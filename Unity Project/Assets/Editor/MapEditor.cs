using UnityEngine;
using UnityEditor;
using System.Collections;

[CustomEditor(typeof(Map))]
public class MapEditor : Editor {
	
	private Map myTarget;
	
	void Awake() {
		// Get target
		myTarget = (Map) target;
	}
	
	public override void OnInspectorGUI() {
		// Draw default instpector as usually for now
		DrawDefaultInspector();
		
		GUILayout.Space( 10 );
		
		// Add an button to start level generation
		if(GUILayout.Button("Generate!")) {
			// Clear up old level
			myTarget.Reset();
			// Start level generation
			myTarget.Generate();
		};
		
		// Add an button to clear generated level
		if(GUILayout.Button("Clear")) {
			// Clear up current level
			myTarget.Reset();
		}
	}
}