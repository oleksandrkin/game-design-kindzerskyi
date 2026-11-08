using System;
using UnityEngine;
 
public class Lever : MonoBehaviour, IInteractable
{
    [SerializeField] private GameObject target;   // платформа, двері, міст
 
    private bool isOn;

    private SpriteRenderer spriteRenderer;

    public void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    public void Interact()
    {
        isOn = !isOn;
        target.SetActive(isOn);

        spriteRenderer.color = isOn ? Color.green : Color.red;
    }
}