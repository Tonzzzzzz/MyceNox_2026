using UnityEngine;
using UnityEditor;

/////////////////////////////////////////////////////////////////////////////////
/// /////////////////////////////////////////////////////////////////////////////
// MyceNox (2026) is my personal RPG-project with turn based combat which I have revised (read broken) more times that I want to even think of.
// The project has been on hold for a while due to panic with everything else that needs doing. I'm returning to it if Keinonen does not descend.
// 
// I thought it would be useful to see my units in the editor and manage their stats in game.
// This way when the project expands I could test the flow of the battle more easily.
// The inner workings of this tool are pretty basic. It doesn't really "kill" any units in-game that are set to 0 HP, for example and is more like in a "concept"-state.
//
// I am addind my old, related UnitController-script to the assingment returns but I have also tried to comment the connected logics extensively here. 
/////////////////////////////////////////////////////////////////////////////////
/// /////////////////////////////////////////////////////////////////////////////

// Tools must be placed in a "Editor"-folder for them to work properly.
public class BattleDebuggerWindow : EditorWindow
{
    private UnitController[] activeUnits;
    private Vector2 scrollPos;

    /////////////////////////////////////////////////////////////////////////////////
    // This creates the "Mycenox" menu any time this sript is compiled (changed). It opens to --> Battle Debugger
    /////////////////////////////////////////////////////////////////////////////////
    [MenuItem("MyceNox/Battle Debugger")] // The next method in the code is automatically wired to the button.
    public static void KeinonenDescents() // Method needs to be static because the Window might not exists in memory yet. 
    {
        GetWindow<BattleDebuggerWindow>("Battle Debugger"); // If no open window exists one is created. If it does, is brought to the front.
                                                            // According to Gemini Unity automatically draws the window to the center of the screen if this is the first time it is activated.
    }

    ///////////////////////////////////////////////////////
    // OnGUI is called continuously to draw the window. It's sort of like the MonoBehaviour's Update(), but instead of every frame it is Unity UI-event driven.
    //////////////////////////////////////////////////////
    private void OnGUI() 
    {
        GUILayout.Label("MyceNox Battle Debugger", EditorStyles.boldLabel);
        
        // The game must be running for the functions of this editor to work.
        if (!Application.isPlaying)
        {
            EditorGUILayout.HelpBox("This tool handles live data only. Enter Play Mode to use it.", MessageType.Warning); 
            return;
        }

        // Find the units for the editor.
        if (GUILayout.Button("Scan Battlefield", GUILayout.Height(30)))
        {
            // Finds every active "Actor" (Player & enemies) in the scene using the UnitController-script they all share.
            activeUnits = FindObjectsByType<UnitController>(FindObjectsSortMode.None);
        }
        
        // No units found! Return!
        if (activeUnits == null || activeUnits.Length == 0)
        {
            EditorGUILayout.HelpBox("No units found!", MessageType.Error); 
            return;
        }

    ///////////////////////////////////////////////////////////////////
    // MAIN LOGIC for the editor:
    ///////////////////////////////////////////////////////////////////
    
        scrollPos = EditorGUILayout.BeginScrollView(scrollPos); // When the "scrollPos" is declared in the beginning it is assigned a value of Vector2(0,0). 
                                                                // Because OnGui is activated when the mouse is moving the new coordinates are saved when the window is moved.
                                                                // I was told by Gemini that Unity automatically remembers the last position of the window, 
                                                                // because if the window is closed the instance is destroyed.

        foreach (var unit in activeUnits)
        {
            if (unit == null) continue;

            // Create visual box for each unit.
            EditorGUILayout.BeginVertical("box");
            
            // .stats refer to the separate and short UnitStatsSO data which holds the character data. (Like max HP and Name) for the Player and Goblin.
            // As all the .stats start null when the unit spawns and are Initialized later (in the UnitController-script) this makes sure that the .Stats actually exist.
            // If for some reason they don't, then the logic pulls the name straight from the Hierarchy and dodges an null-error.
            
            string unitName = unit.Stats != null ? unit.Stats.unitName : unit.gameObject.name; // I'm still very bad at writing this tightly packed. Gemini actually improved my if-else statements to this when asked.
            string status = unit.IsDead ? "DEAD" : "ALIVE";
            
            
            GUILayout.Label(unitName + " " + status, EditorStyles.boldLabel);

            if (!unit.IsDead) 
            {
                // Health Management
                EditorGUILayout.BeginHorizontal();
                GUILayout.Label("HP: " + unit.CurrentHealth + "/" + unit.Stats.maxHealth, GUILayout.Width(120)); //// 
                
                // The methods (TakeDamage() & Heal()) used here exists inside a "UnitController"-script in the game
                
                if (GUILayout.Button("-10 HP")) unit.TakeDamage(10, null, false); 
                if (GUILayout.Button("+10 HP")) unit.Heal(10);
                if (GUILayout.Button("KILL")) unit.TakeDamage(9999, null, false);
                
                // The first value in the method is self explanatory. There is no any logic here to actually remove any units from in-game so far though.

                // The "null" is the designated attacker but there is no need for any in the editors case. 
                // (I'm thinking of building a "flanking"-logic to the game where the attacker "locks" with the target and if some other unit attacks the locked unit is penalized for more damage. 
                // As of now there exists only one player and enemy (Goblin) unit in the game so it's a work in progress.)

                //The "false" on the other hand is used in the case of a "power attack" which there is no reason for the tool to be. 
                // (I am building a "Stance system" that lets the player attack several times in a turn, but each time he's "state" gets more "exposed" from: Defending-->Acted-->Overextended-->EXPOSED.
                // If EXPOSED, then there is a chance that the next attack of the enemy is a "power attack" and there is a 90% chance that the player loses his next turn if it connets. )

                EditorGUILayout.EndHorizontal();

                // Stance Management. 
                // The "Stance system" and "EXPOSED" are explained in the previous comments.
                EditorGUILayout.BeginHorizontal();
                GUILayout.Label("Stance: " + unit.CurrentStance, GUILayout.Width(120)); 
                
                if (GUILayout.Button("Force Action (+EXPOSED)"))
                {
                    unit.RegisterAction(); // Pushes a Unit a step towards EXPOSED.
                }
                if (GUILayout.Button("Reset Turn"))
                {
                    unit.ResetTurn(); // Returns unit back to "Defending".
                }
                
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndVertical();
            GUILayout.Space(5);
        }

        EditorGUILayout.EndScrollView();

        // Forces the window to visually update immediately if health changes in the actual game. Used because OnGui() is UI-event driven and might not update unless mouse is wiggled ect.
        Repaint(); 
    }
}