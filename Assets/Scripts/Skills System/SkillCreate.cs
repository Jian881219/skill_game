﻿using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// Creates a custom skill
// This system uses Skill Gems that contains preset variables and combines them into a single skill. Each skill will
// require skill gems that cover all variables of the skill.
public class SkillCreate : MonoBehaviour {

    private const int MAX_GEMTYPE_COUNT = 20;

    [System.Serializable]
    public class EffectGraphic {
        public Vector3 castOffset;
        public AnimationClip cast;
        public AnimationClip hit;
    }
    [System.Serializable]
    public class EffectsList {
        public Tier tier;
        public List<EffectGraphic> effects;

        public EffectGraphic RandomEffect {
            get {
                return effects[UnityEngine.Random.Range(0,effects.Count)];
            }
        }
    }
    // List of all available castEffects and hitEffects based on tier
    public List<EffectsList> skillEffects;

    [Header("Object References")]
    public GameObject skillsUI;
    public InputField nameField;
    public InputField descriptionField;
    public RectTransform contentTrans;
    public GameObject selectSkillGems;
    public GameObject saveSkill;
    public GameObject inventoryItem;
    public GameObject characterIcon;

    private Text skillInfoText;                 // Expected skill information from crafting
    private Text chanceText;                    // Text of chance to craft
    private RectTransform charactersRect;       // Rect Transform of where to place character icons for selection

    private float chanceToCraft = 100f;                                                         // Current chance to successfully craft the skill
    private Skill toCraft = null;                                                               // Current skill settings by combining skillGems
    private List<SkillGem> skillGems = new List<SkillGem>();                                    // Currently selected skill gems to combine
    private List<GameObject> gemsUI = new List<GameObject>();                                   // UI objects for gems
    private List<Inventory.InventoryItem> gems = new List<Inventory.InventoryItem>();           // All skillGems in player's inventory

    void Awake(){
        // Look for skillInfoText
        Transform trans = selectSkillGems.transform.Find("Craft Skill Info");
        if ( trans != null ) skillInfoText = trans.GetChild(0).GetComponent<Text>();

        // Look for chanceText
        trans = selectSkillGems.transform.Find("Chance of Success");
        if ( trans != null ) chanceText = trans.GetComponent<Text>();

        // Look for charactersRect
        trans = saveSkill.transform.Find("Characters");
        if ( trans != null ) charactersRect = trans as RectTransform;
    }

    // By default, this script is inactive on the UI canvas. To start crafting a skill, enable the gameObject this script is on
    void OnEnable(){
        selectSkillGems.SetActive(true);
        saveSkill.SetActive(false);

        GenerateList();
    }

    // UI Methods
    // Methods called from UI events
    // Display crafting animation
    public void Craft(){
        // Make sure theres a name and description available
        if ( nameField.text == "" || descriptionField.text == "" ){
            Debug.Log("You need to input a name or description!");
            return;
        }

        // Chance to craft skill depending on amount of skill gems and tiers of each skill gem
        // Remove skill gems from inventory
        foreach (SkillGem gem in skillGems){
            Player.instance.inventory.RemoveItem(gem, 1);
        }

        // Clear skillGems for next craft
        skillGems = new List<SkillGem>();

        if ( UnityEngine.Random.Range(0,100) < chanceToCraft ){
            // Crafting successful
            Debug.Log("Craft successful!");
            selectSkillGems.SetActive(false);
            saveSkill.SetActive(true);
        } else {
            // Crafting failed
            Debug.Log("Craft failed...");
            UpdateSkill();
            GenerateList();
        }
    }
    // Give skill to character
    public void SaveSkillToCharacter(){
        // Make sure we have a skill to save
        if ( toCraft != null ){
            // Check if there is a duplicate skill
            Skill duplicate = Player.instance.character.skills.Where<Skill>( (s) => s.name == toCraft.name).FirstOrDefault();
            if ( duplicate == null ){
                toCraft.name = nameField.text;
                toCraft.description = descriptionField.text;

                Debug.Log(Player.instance.character.name + " gained the skill, " + toCraft.name);
                Player.instance.character.AddSkill(toCraft);
                skillsUI.SetActive(true);
                gameObject.SetActive(false);
            } else {
                // There cannot be skills with the same name, Replace or change name of skill
                Debug.Log(duplicate.name + " already exists in skill library. Input another name for the skill.");
                selectSkillGems.SetActive(true);
                saveSkill.SetActive(false);
            }
        }
    }
    // Save skill as a rune to player's inventory
    public void SaveSkillToRune(){
        // Make sure we have a skill to save
        if ( toCraft != null ){
            Player.instance.inventory.AddItem(new Rune(toCraft),1);
            gameObject.SetActive(false);
            skillsUI.SetActive(true);
        }
    }
    // Add skillGem to pool based on index from gems list
    public void Select(int index){
        SkillGem skillGem = gems[index].item as SkillGem;

        // Select/Deselect skillGem
        if ( skillGems.Contains(skillGem) ){
            skillGems.Remove(skillGem);
            gemsUI[index].GetComponent<Image>().color = Color.white;
        } else {
            int skillGemVal = (int)skillGem.gemType;

            // Check if skillGem has any conflicting gems, if so remove them
            for (int i = skillGems.Count-1; i >= 0; i--){
                int gemVal = (int)skillGems[i].gemType;

                if ( skillGemVal < 10 ){
                    // skillGem is an element type skillGem
                    // Check if this gem is also an element type
                    if ( gemVal < 10 ){
                        // Remove gem
                        skillGems.RemoveAt(i);
                    }
                } else if ( skillGemVal == 10 || skillGemVal == 11 ){
                    // skillGem is a damage or heal effect
                    // Check if this gem is also a damage or heal effect
                    if ( gemVal == 10 || gemVal == 11 ){
                        // Remove gem
                        skillGems.RemoveAt(i);
                    }
                } else if ( skillGemVal == gemVal ){
                    // Remove skill gem since its of the same type
                    skillGems.RemoveAt(i);
                }
            }

            skillGems.Add(skillGem);

            for (int i = 0; i < gems.Count; i++){
                if ( skillGems.Contains(gems[i].item as SkillGem) ){
                    gemsUI[i].GetComponent<Image>().color = Color.yellow;   
                } else {
                    gemsUI[i].GetComponent<Image>().color = Color.white;   
                }
            }
        }

        UpdateSkill();
    }
    // Add a new randomly generated skillGem
    public void AddSkillGem(){
        int randomNum = UnityEngine.Random.Range(1,MAX_GEMTYPE_COUNT);

        // Since skillGem category is based on range of ints, check which gem should be created whether
        // randomNum is less than or equal to the category num
        int randomTier = UnityEngine.Random.Range(0,100);
        Tier tier = Tier.common;
        if ( randomTier <= 5 ) tier = Tier.legendary;
        else if ( randomTier <= 25 ) tier = Tier.unique;
        else if ( randomTier <= 60 ) tier = Tier.rare;

        SkillGemType gemType = (SkillGemType)Enum.ToObject(typeof(SkillGemType),randomNum);
        string gemName = ((SkillGemType)randomNum).ToString();
        Debug.Log("Generating " + tier + " " + gemName);
        if ( randomNum <= (int)SkillGemCategory.elementGem ){

            // Grab random castEffect and hitEffect
            EffectsList effectsList = GetEffectsList(tier);
            EffectGraphic effect = effectsList.RandomEffect;

            string castEffect = "", hitEffect = "";
            castEffect = effect.cast.name;
            hitEffect = effect.hit.name;
            ElementGem gem = new ElementGem( gemName, 
                                             "Random generated skillGem", 
                                             "", 
                                             tier,
                                             ItemType.skillGem,
                                             gemType,
                                             effect.castOffset,
                                             castEffect,
                                             hitEffect );
            Player.instance.inventory.AddItem(gem,1);
        } else if ( randomNum <= (int)SkillGemCategory.damageEffect ){
            float min = UnityEngine.Random.Range(1,100);
            float max = min + UnityEngine.Random.Range(3,50);

            EffectGem gem = new EffectGem( gemName,
                                           "Random generated skillGem",
                                           "",
                                           tier,
                                           ItemType.skillGem,
                                           gemType,
                                           new Damage(min, max, 1, false));
            Player.instance.inventory.AddItem(gem,1);
        } else if ( randomNum <= (int)SkillGemCategory.healEffect ){
            EffectGem gem = new EffectGem( gemName,
                                           "Random generated skillGem",
                                           "",
                                           tier,
                                           ItemType.skillGem,
                                           gemType,
                                           new Heal(UnityEngine.Random.Range(10,100), false));
            Player.instance.inventory.AddItem(gem,1);
        } else if ( randomNum <= (int)SkillGemCategory.buffEffect ){
            EffectGem gem = new EffectGem( gemName,
                                           "Random generated skillGem",
                                           "",
                                           tier,
                                           ItemType.skillGem,
                                           gemType,
                                           new Buff(false,
                                                    60,
                                                    new AttributeStats(),
                                                    new CharStats(),
                                                    new CombatStats()));
            Player.instance.inventory.AddItem(gem,1);
        } else if ( randomNum <= (int)SkillGemCategory.statusEffect ){
            EffectGem gem = new EffectGem( gemName,
                                           "Random generated skillGem",
                                           "",
                                           tier,
                                           ItemType.skillGem,
                                           gemType,
                                           new StatusEffect());
            Player.instance.inventory.AddItem(gem,1);
        }

        GenerateList();
    }

    // Switch UI to skills showcase
    public void OpenSkillsUI(){
        skillsUI.SetActive(true);
        gameObject.SetActive(false);
    }

    // Update the skill to craft
    // For UI purposes
    private void UpdateSkill(){
        // Clear last settings of the crafting skill
        toCraft = new Skill();

        // Apply name and description to skill
        toCraft.name = nameField.text;
        toCraft.description = descriptionField.text;

        // Apply skillGems to skill
        chanceToCraft = 0f;
        foreach (SkillGem gem in skillGems){
            gem.ApplyTo(toCraft);

            chanceToCraft += (int)gem.tier/skillGems.Count;
        }

        // Update success chance to craft
        chanceText.text = chanceToCraft + "% chance of success";

        // Update Skill Info UI
        string text = "";

        // Element and Graphics
        text += "Element: " + toCraft.elementType.ToString() + "\n";
        text += "Cast Effect: " + (toCraft.castEffect != "" ? toCraft.castEffect : "None") + "\n";
        text += "Hit Effect: " + (toCraft.hitEffect != "" ? toCraft.hitEffect : "None") + "\n";

        // Target Count
        text += "Target Count: " + toCraft.targetCount + "\n";

        // Effects
        text += "Effects: ";
        if ( toCraft.effects.Count > 0 ){
            text += "\n";
            for (int i = 0; i < toCraft.effects.Count; i++){
                text += (i+1) + ".) " + toCraft.effects[i].info + "\n";
            }
        } else {
            text += "None";
        }

        skillInfoText.text = text;
    }
    // Generate UI list of skillGems available to craft with
    private void GenerateList(){
        // Clear UI objects if any
        if ( gemsUI.Count > 0 ){
            for (int i = gemsUI.Count-1; i >= 0; i--){
                Destroy(gemsUI[i]);
            }
            gemsUI = new List<GameObject>();
        }
        
        skillGems = new List<SkillGem>();

        IEnumerable<Inventory.InventoryItem> gemItems = Player.instance.inventory.items.Where<Inventory.InventoryItem>( (ii) => ii.item.itemType == ItemType.skillGem);
        if ( gemItems == null ) return;
        gems = gemItems.ToList();

        // Create UI object of each skillGem in list
        float height = ((RectTransform)inventoryItem.transform).rect.height;
        contentTrans.sizeDelta = new Vector2(contentTrans.sizeDelta.x, height*skillGems.Count);
        float startY = contentTrans.rect.height/2.00f - height/2.00f;
        for (int i = 0; i < gems.Count; i++){
            GameObject o = Instantiate(inventoryItem);
            o.transform.SetParent(contentTrans);
            o.transform.localScale = Vector3.one;

            // Position ui element in scroll view
            RectTransform rt = (RectTransform)o.transform;
            rt.anchoredPosition = new Vector2(0f,startY-i*height);

            // Icon
            Image icon = (Image) o.transform.GetChild(0).GetComponent<Image>();
            icon.sprite = gems[i].item.Icon;

            // Name
            Text nameText = (Text) o.transform.GetChild(1).GetComponent<Text>();
            nameText.text = gems[i].item.name;

            // Amount
            Text amount = (Text) o.transform.GetChild(2).GetComponent<Text>();
            string text = "";
            SkillGemType gemType = ((SkillGem)gems[i].item).gemType;
            if ( (int)gemType <= (int)SkillGemCategory.elementGem ){
                text = "Element";
            } else if ( (int)gemType <= (int)SkillGemCategory.damageEffect ){
                text = "Damage";
            } else if ( (int)gemType <= (int)SkillGemCategory.healEffect ){
                text = "Heal";
            } else if ( (int)gemType <= (int)SkillGemCategory.buffEffect ){
                text = "Buff";
            } else if ( (int)gemType <= (int)SkillGemCategory.statusEffect ){
                text = "Status";
            }
            amount.text = text;

            // Add listener when selecting this skillGem
            Button button = o.GetComponent<Button>();
            int index = i;
            button.onClick.AddListener(() => Select(index));

            gemsUI.Add(o);
        }
    }
    // Return list of effects based on tier param
    private EffectsList GetEffectsList(Tier tier){
        return skillEffects.Where<EffectsList>((se) => se.tier == tier).FirstOrDefault();
    }
}