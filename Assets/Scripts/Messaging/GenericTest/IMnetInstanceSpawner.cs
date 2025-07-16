using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IMnetInstanceSpawner
{
    // Manager saa viestin ett‰ pit‰‰ spawnata objekti. Viestiss‰ molemmat id ja objectID annetaa spawnerille.
    // Spawner palauttaa luodun/poolatun objectin ja manageri lis‰‰ sen synkattaviin k‰ytt‰en instanceID:t‰ indexin‰
    public MnetObject SpawnRequest(int objectID);

    // Manageri saa viestin ett‰ objekti poistuu. Se instanceID avulla poistaa objektin synkattavista ja l‰hett‰‰ objektin spawnerille
    // Spawner pist‰‰ objektin takaisin pooliin tai miten sitten toimiikaan
    public void DespawnRequest(MnetObject objectToDespawn);

    // Antaa kopiot pelaaja objekteista jotka client sitten player numerolla osaa sijottaa oikeaan kohtaan molempiin taulukoihin
    public MnetObject[] GetPlayerObjects();

    public void Tick();

    public MnetObject[] GetActiveObjects();
}
