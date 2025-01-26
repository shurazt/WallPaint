using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObjectClick : MonoBehaviour
{
   public ChangeTexture changeTexture;

   public void OnMouseDown()
   {
      changeTexture.ChangeTextureObject = gameObject;
   }
}
