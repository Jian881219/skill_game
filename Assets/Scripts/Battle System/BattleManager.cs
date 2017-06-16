﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Handles turn by turn combat between players and/or ai
// This system uses the speed of each character to determine which character takes priority.
// Note: All monster sprites MUST be below the monsterSpawn width and height divided by MAX_MONSTER_COUNT
public class BattleManager : MonoBehaviour {

    private const int MAX_MONSTER_COUNT = 5;

    public float playerSpacing = 2f;            // Space between player characters
    public BattlePhase battlePhase;             // Current phase of battle

    [Header("Object References")]
    public GameObject targetable;               // Object references for a targetable object
    public GameObject battleLog;                // Logs every action performed by a character
    public GameObject battleMenu;               // Battle UI elements in overlay canvas
    public RectTransform monsterSpawn;          // Position in camera of where monsters will appear
    public Transform playerStartPos;            // Position of where to place the first player character

    private AIManager aiManager;                // Handles AI
    private Monster[] monsterPool;              // Pool of all monsters that the player might face depending on region
    private List<Monster> monsters;             // List of monster objects in battle
    private List<CharacterObj> playerChars;     // List of all player objects in battle

    private GameObject playerActionSelect;      // BattleMenu for Action select
    private GameObject list;                    // List in BattleMenu for skills/consumables
    private int selectedCharacter = 0;          // Currently selected character for the player to select an action for
    private List<CharacterAction> actions;      // List of all actions that will be performed in a battle round

    private CharacterAction CurrentAction {
        get {
            return actions[actions.Count-1];
        }
    }

    void Awake(){
        // Fill monsterPool with monsters depending on region
        monsterPool = Resources.LoadAll<Monster>(Player.instance.RegionPath);

        // Initialize managers
        aiManager = new AIManager();
    }
    void Start(){
        GenerateCharacters();
        PlayerAction();
    }

    // Create player characters/monsters for battle
    private void GenerateCharacters(){
        // Instantiate player characters based on character name
        playerChars = new List<CharacterObj>();
        for (int i = 0; i < Player.instance.characters.Count; i++){
            GameObject o = (GameObject) Instantiate(Resources.Load("Characters/"+Player.instance.characters[i].name));

            CharacterObj co = o.GetComponent<CharacterObj>();
            co.character = Player.instance.characters[i];

            playerChars.Add(co);
        }

        // Space player characters based on startPos
        // Assume that all characters are of same width
        float width = ((RectTransform)playerChars[0].transform).rect.width;
        for (int i = 0; i < playerChars.Count; i++){
            playerChars[i].transform.position = playerStartPos.position + new Vector3(i*(width+playerSpacing),0,0);
        }

        // Instantiate monster objects
        monsters = new List<Monster>();
        int monsterCount = Random.Range((int)Player.instance.currentRegion, MAX_MONSTER_COUNT);
        width = 0f;
        for (int i = 0; i < monsterCount; i++){
            Monster o = Instantiate(monsterPool[Random.Range(0,monsterPool.Length)]);
            o.transform.SetParent(monsterSpawn);

            width += ((RectTransform)o.transform).rect.width;

            monsters.Add(o);
        }

        // Space monsters based on size of each
        float space = (monsterSpawn.rect.width - width) / (monsterCount+1);
        float x = -monsterSpawn.rect.width/2.00f;
        for (int i = 0; i < monsterCount; i++){
            float w1 = i > 0 ? ((RectTransform)monsters[i-1].transform).rect.width/2.00f : 0f;
            float w2 = ((RectTransform)monsters[i].transform).rect.width/2.00f;
            x += w1 + space + w2;

            monsters[i].transform.localPosition = new Vector3(x,0,0);
        }
    }
    // Setup UI to allow player to perform actions for their characters
    private void PlayerAction(){
        // Display actions
        battleLog.SetActive(false);
        battleMenu.SetActive(true);
        playerActionSelect.SetActive(true);
        list.SetActive(false);

        // Reset selectedCharacter
        selectedCharacter = 0;
        
        // Clear actionsList
        actions = new List<CharacterAction>();
    }
    // Move to next playerCharacter
    private void NextPlayerCharacter(){
        selectedCharacter++;

        // Check if the player selected actions for all their characters
        if ( selectedCharacter >= playerChars.Count ){
            // End player turn
            battlePhase = BattlePhase.enemyTurn;
        } else {
            battlePhase = BattlePhase.actionSelect;
        }
    }

    // All actions a player character can make.
    // Perform a basic attack to a target.
    public void Attack(){
        actions.Add( new CharacterAction(playerChars[selectedCharacter], ActionType.attack) );
        battlePhase = BattlePhase.targetSelect;
    }
    // Cast a spell on a target or targets.
    public void Cast(){
        
    }
    // Use an item on a target or targets
    public void Use(){

    }
    // Run away from battle. Automatically ends players turn with a chance to e xit battle. 
    public void Run(){

    }

    // Set target for selected character based on index of targetable
    public void SetTarget(int index){
        if ( battlePhase != BattlePhase.targetSelect ) return;

        if ( index >= playerChars.Count ){
            // Player is targetting a monster
            index = index - playerChars.Count;

            if ( CurrentAction.targets.Contains(monsters[index]) ){
                // Remove target
                CurrentAction.targets.Remove(monsters[index]);
            } else {
                // Add target
                CurrentAction.targets.Add(monsters[index]);
            }
        } else {
            // Player is targetting a player character
            if ( CurrentAction.targets.Contains(playerChars[index]) ){
                // Remove target
                CurrentAction.targets.Remove(playerChars[index]);
            } else {
                // Add target
                CurrentAction.targets.Add(playerChars[index]);
            }
        }

        // Check if player selected enough targets
        if ( CurrentAction.IsAoe ){
            if ( CurrentAction.action == ActionType.cast ){
                if ( CurrentAction.targets.Count >= CurrentAction.skill.targetCount ){
                    NextPlayerCharacter();
                }
            } else {
                if ( CurrentAction.targets.Count >= CurrentAction.usable.targetCount ){
                    NextPlayerCharacter();
                }
            }
        } else {
            // Single target only
            NextPlayerCharacter();
        }
    }
}