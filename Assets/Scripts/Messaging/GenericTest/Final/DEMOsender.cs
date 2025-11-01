using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DEMOsender : MonoBehaviour
{
    public bool GO;
    private DEMOseriObj obj;
    private MnetNetwork ep;

    private void Awake()
    {
        obj = GetComponent<DEMOseriObj>();
        ep = GetComponent<MnetNetwork>();
    }
    private void Update()
    {
        if (GO)
        {
            obj.level.Value = 420;
            obj.helloWorld.Value = "Hello World!";
            obj.dickLength.Value = 21f;
            obj.longTest.Value = Loreal();
            obj.XavierTest.Value = 1969;
            GO = false;
            ep.worldPackets.packetToWriteOn.ClosePacket();
        }
    }

    public string Loreal()
    {
        return "Lorem ipsum dolor sit amet, consectetur adipiscing elit. Quisque eu cursus enim. In congue at diam vel tempus" +
            ". Pellentesque dui mauris, venenatis molestie euismod a" +
            "c, tristique ut massa. Duis ullamcorper pretium massa et fringilla. Nam ultrices mauris sed egesta" +
            "s ultricies. Proin a laoreet odio. Vestibulum malesuada risus sit amet elit vehicula, vel ornare diam maximus. Phasel" +
            "lus eu pellentesque nulla, sit amet rhoncus ex. Donec accumsan neque et nulla tempor lobortis. Donec pulvinar luctus felis ut " +
            "sagittis. Aliquam gravida dignissim fermentum. Pellentesque vehicula accumsan sem, ut venenatis urna dictum nec. Pellentesque augue lac" +
            "us, elementum vitae arcu et, dapibus molestie justo. Aliquam felis tortor, venenatis tincidunt pharetra hendrerit, matt" +
            "is quis tellus. Mauris efficitur elementum leo non auctor. Vestibulum vestibulum at dui id egestas. Nu" +
            "lla id ligula in leo tincidunt elementum in id nisi. Phasellus eu dictum ex. Etiam est felis, cond" +
            "imentum a molestie sit amet, luctus in massa. Cras lobortis quam id magna faucibus pellentesque." +
            " Morbi imperdiet malesuada leo vitae semper. Nunc sed purus cursus, porttitor justo a, volutpat an" +
            "te. Nam sagittis nunc lacus, in sollicitudin nulla volutpat vel. In nec ligula quis felis sollici" +
            "tudin viverra sed at dolor. Mauris tempor pellentesque turpis id bibendum. Phasellu" +
            "s felis eros, iaculis nec magna nec, cursus convallis est. Praesent orci felis, port" +
            "titor in gravida dignissim, posuere eu magna. Morbi accumsan sed quam non bibendum. Q" +
            "uisque viverra imperdiet nisi, a sodales orci venenatis quis. Proin varius purus id ve" +
            "lit fringilla, id pretium justo rhoncus. Sed elementum est sit amet odio" +
            " dapibus iaculis. Nulla dictum enim nisi, id molestie magna accumsan sit amet." +
            " Integer tempor risus in augue lobortis, eget aliquet justo cursus. Lorem ipsum do" +
            "lor sit amet, consectetur adipiscing elit. Duis commodo arcu urna, in aliquet urna ma" +
            "lesuada viverra. In vulputate efficitur rhoncus. Phasellus eget tincidunt lacus. Curabi" +
            "tur in ac.ggg";
    }
}
