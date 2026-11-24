using UnityEngine;

// Sənin yatağın: yat və növbəti səhərə qədər dincəl (bitkilər böyüyür, ürəklər dolur).
public class MMBed : MMInteractable
{
    public override void Interact(MMPlayer player)
    {
        MMGame.I.Sleep();
    }
}
