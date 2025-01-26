using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChangeTexture : MonoBehaviour
{
    public List<Texture> allTextures;
    public GameObject ChangeTextureObject;
    
    public void ChangeTextureWithOutObject(string textureName)
    {
        if (ChangeTextureObject == null)
        {
            Debug.Log("Wall is null");
        }

        Texture currentTexture = allTextures.Find(x => x.name == textureName);  
        Renderer renderer = ChangeTextureObject.GetComponent<Renderer>();
      
        if (currentTexture && renderer)   
        {
            renderer.material.color = Color.white;
            renderer.material.mainTexture = currentTexture;
        }
        else                                    
        {
            renderer.material.color = Color.red;
        }
    }

}