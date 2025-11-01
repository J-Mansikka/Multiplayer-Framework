using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IMnetUserInstanceSpawner
{
    // Manager saa viestin ett‰ pit‰‰ spawnata objekti. Viestiss‰ molemmat id ja objectID annetaa spawnerille.
    // Spawner palauttaa luodun/poolatun objectin ja manageri lis‰‰ sen synkattaviin k‰ytt‰en instanceID:t‰ indexin‰
    public WANHAMnetObject SpawnRequest(int objectID);

    // Manageri saa viestin ett‰ objekti poistuu. Se instanceID avulla poistaa objektin synkattavista ja l‰hett‰‰ objektin spawnerille
    // Spawner pist‰‰ objektin takaisin pooliin tai miten sitten toimiikaan
    public void DespawnRequest(WANHAMnetObject objectToDespawn);

    // Antaa kopiot pelaaja objekteista jotka client sitten player numerolla osaa sijottaa oikeaan kohtaan molempiin taulukoihin
    public WANHAMnetObject[] GetPlayerObjects();

    // Mik‰ helvetti t‰‰ on? Spawnit tapahtuu vaan tickein‰ kai joo mut miten se edes kutsutaa ku en tie millanen luokka t‰‰ tulee olee? Jeesus
    public void Tick();

    public WANHAMnetObject[] GetActiveObjects();
}
