using UnityEngine;

[CreateAssetMenu(fileName = "NewCharacterData", menuName = "MyceNox/Character Data")]
public class CharacterData : ScriptableObject
{
    public string characterName;
    public int maxHealth;
    public int attackPower;
    public Sprite artwork;
    // Add more stats here later (Defense, Speed, etc.)
}