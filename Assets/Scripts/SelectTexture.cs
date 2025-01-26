using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SelectTexture : MonoBehaviour
{
    [SerializeField] RawImage imageTexture;
    List<Texture> textures = new List<Texture>();
    int currentNumTexture = 0;
    public Texture currentTexture;

    void Start()
    {
        textures = GetComponent<ChangeTexture>().allTextures;
        currentTexture= textures[currentNumTexture];
        imageTexture.texture = currentTexture;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space)) // смена текстуры
        {
            currentNumTexture++;
            if (currentNumTexture >= textures.Count) currentNumTexture = 0;
            currentTexture = textures[currentNumTexture];
            imageTexture.texture = currentTexture;
        }
    }
}