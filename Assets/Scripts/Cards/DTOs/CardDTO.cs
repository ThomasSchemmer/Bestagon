using Unity.Collections;
using UnityEngine;

/** Helper class to transfer the cards between scenes. Only contains actually important data 
 * aka no visuals (as this is unnecessary to save and will be regenerated anyway) 
 */
public abstract class CardDTO
{      
    public enum Type
    {
        Building,
        Event
    }

    [SaveableBaseType]
    public int PinnedIndex = -1;
    [SaveableEnum]
    public Card.CardState State = Card.CardState.DEFAULT;
    [SaveableBaseType]
    public bool bWasUsedUp = false;
    [SaveableEnum]
    public Type CardType;

    public abstract Type GetCardType();

    public static CardDTO CreateFromCard(Card Card) {

        CardDTO DTO = null;
        if (Card is BuildingCard)
            DTO = new BuildingCardDTO(Card);

        if (Card is EventCard)
            DTO = new EventCardDTO(Card);

        if (DTO == null)
        {
            throw new System.Exception("Could not create DTO for Card");
        }

        DTO.PinnedIndex = Card.GetPinnedIndex();
        DTO.State = Card.GetState();
        DTO.bWasUsedUp = Card.WasUsedUpThisTurn();
        DTO.CardType = DTO.GetCardType();

        return DTO;
    }

    public abstract bool ShouldBeDeleted();

}
