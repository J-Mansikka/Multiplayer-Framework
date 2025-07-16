using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IMnetInstanceManager
{
    // Spawneri p‰‰tt‰‰ luoda uuden objektin tai hakea sen poolista. Se kutsuu t‰m‰n metodin managerissa objektilla
    // Manager ottaa objektin vastaan ja antaa sille instanceID:n. Se toimii indexin‰ kun manageri lis‰‰ sen synkattavien taulukkoon.
    // Manager sitten luo uuden l‰htev‰n instance viestin eli Spawn, annettu instanceID, objectID
    // Remote manager sitten n‰ill‰ ID luvuilla toistaa spawnin
    public void LocalSpawn(MnetObject objectBeingSpawned);

    // Objekti tuhoutui tai on maannut kuolleena niin kauan ett‰ se palaa spawnerille. Spawn antaa t‰m‰n objektin Managerille ett‰ se voidaan h‰vitt‰‰ myˆs toisessa p‰‰ss‰.
    // Manager ottaa objektin vastaan ja poistaa sen synkattavista.
    // Manager luo viestin jotta objekti voidaan poistaa toisessakin p‰‰ss‰ (Despawn, instanceID)
    // Remote manager toistaa toiminnon.
    public void LocalDespawn(MnetObject objectBeingDespawned);
}
