using System;
using System.Collections.Generic;
using Jypeli;
using Jypeli.Assets;
using Jypeli.Controls;
using Jypeli.Widgets;

/// @author Aleksanteri Strömberg
/// @version 20.11.20
/// <summary>
/// Ohjelmointi 1 kurssin harjoitustyö, Ninja of Japan
/// </summary>
public class ht : PhysicsGame
{
    private PlatformCharacter pelaaja;
    private Cannon ase;
    EasyHighScore topTen = new EasyHighScore();
    private Timer restartDelay;
    private Timer mainMenuDelay;
    IntMeter pisteLaskuri;
    IntMeter elamaLaskuri;
    private Image pelaajanKuva = LoadImage("pelaaja.png");
    private Image kruunuKuva = LoadImage("kruunu.png");
    private Image tahtiKuva = LoadImage("tahti.png");
    private Image esteKuva = LoadImage("este.png");
    private Image vihuKuva = LoadImage("vihu.png");
    private Image heittoTahtiKuva = LoadImage("heittotahti.png");
    private Timer ajastin = new Timer();
    private const double nopeus = 100;
    private const double hyppynopeus = 600;
    private double valiAika = 0;
    private int keratytPisteet = 0;
    private int elamienLkm = 3;
    private int tahtienLkmKentassa = 0;
    private int nykyinenKentta = 0;
    private string[] kentat = new string[] { "kentta.txt", "kentta2.txt", "kentta3.txt" };

    public override void Begin()
    {
        //SetWindowSize(1920, 1080, true);
        SetWindowSize(1024, 768, false);
        MainMenu();
    }
    /// <summary>
    /// Aliohjelma, jossa on luotu ja määritelty pelin päävalikko.
    /// </summary>
    private void MainMenu()
    {
        ClearAll();
        MultiSelectWindow MainMenu = new MultiSelectWindow("Main menu", "Start game", "TOP 10", "Quit");
        MainMenu.AddItemHandler(0, UusiPeli);
        MainMenu.AddItemHandler(1, NaytaTopTenMenussa);
        MainMenu.AddItemHandler(2, ConfirmExit);
        Add(MainMenu);
    }
    /// <summary>
    /// Aliohjelma, jossa päävalikossa painamalla nappia "TOP 10" saa näkyville TOP 10 listauksen.
    /// </summary>
    private void NaytaTopTenMenussa()
    {
        topTen.Show();
        topTen.HighScoreWindow.Closed += delegate { MainMenu(); };
    }

    /// <summary>
    /// Aliohjelma, jossa on toteutettu TOP 10 listaus, kertoimet tietylle ajalle, algoritmi miten pisteet määyräytyvät. Algoritmissa otetaan huomioon pelaajan aika, sille tietty kerroin sekä kerätyt pisteet.
    /// </summary>
    private void TopTenLista()
    {
        int[,] kerroinTaulukko = { {30, 8},
                            {35, 7 },
                            {40, 6 },
                            {45, 5},
                            {50, 4},
                            {55, 3},
                            {60, 2}};

        int eka = kerroinTaulukko[0, 0];
        int kerroin = 0;
        for (int i = 1; i < kerroinTaulukko.Length/2; i++)
        {
            if (kerroinTaulukko[i, 0] < valiAika && valiAika > eka)
            {
                kerroin = kerroinTaulukko[i - 1, 1];
            }
        }
        int kokPisteet = kerroin * keratytPisteet;

        topTen.EnterAndShow(kokPisteet);
        topTen.HighScoreWindow.Closed += delegate { MainMenu(); };
    }

    /// <summary>
    /// Aliohjelma, jossa luodaan peliin ajastin, jota vastaan taistellaan.
    /// </summary>
    private void LuoPeliKello()
    {
        ajastin.CurrentTime = valiAika;
        ajastin.Start();

        Label ajastinNaytto = new Label();
        ajastinNaytto.Position = new Vector((Window.ClientBounds.Width - ajastinNaytto.Width) / 2, -1 * (Window.ClientBounds.Height - ajastinNaytto.Height) / 2);
        ajastinNaytto.TextColor = Color.Black;
        ajastinNaytto.DecimalPlaces = 2;
        ajastinNaytto.BindTo(ajastin.SecondCounter);
        Add(ajastinNaytto);
    }

    /// <summary>
    /// Aliohjelma, joka laskee pelaajan pisteet pelin edetessä
    /// </summary>
    private void PisteTilastot()
    {
        pisteLaskuri = new IntMeter(keratytPisteet);

        Label hud = new Label();
        hud.X = Screen.Left + 45;
        hud.Y = Screen.Bottom + 62;
        hud.TextColor = Color.Black;
        hud.Color = Color.White;
        hud.Title = "Pojot";
        hud.BindTo(pisteLaskuri);
        Add(hud);
    }
    /// <summary>
    /// Aliohjelma, jossa pelin aikana näkyy pelaajien elämien lukumäärä.
    /// </summary>
    private void ElamienMaara()
    {
        elamaLaskuri = new IntMeter(elamienLkm);

        Label elamaHud = new Label();
        elamaHud.X = Screen.Left + 50;
        elamaHud.Y = Screen.Bottom + 37;
        elamaHud.TextColor = Color.Black;
        elamaHud.Color = Color.White;
        elamaHud.Title = "Elämät";
        elamaHud.BindTo(elamaLaskuri);
        Add(elamaHud);

    }
    /// <summary>
    /// Aliohjelma, jossa luodaan aina läpäistyn kentän jälkeen seuraava kenttä. Asetetaan nykyinen kentta yhtä suuremmaksi ja kutsutaan aliohjelmaa lataaKenttaUudelleen.
    /// </summary>
    private void SeuraavaKentta()
    {
        nykyinenKentta = nykyinenKentta + 1;
        LataaKenttaUudelleen();
    }
    /// <summary>
    /// Aliohjelma, joka luo kentän tietyllä rakenteella kullakin kierroksella, parametrin kentta avulla.
    /// </summary>
    /// <param name="kentta">parametrina tuodaan aliohjelmalle tietty kentta</param>
    private void LuoKentta(string kentta)
    {

        TileMap taso = TileMap.FromLevelAsset(kentta);
        taso.SetTileMethod('N', LisaaPelaaja);
        taso.SetTileMethod('k', LisaaKruunu);
        taso.SetTileMethod('v', LisaaVihollinen);
        taso.SetTileMethod('e', LisaaEste);
        taso.SetTileMethod('#', LisaaTaso);
        taso.SetTileMethod('*', LisaaTahti);
        taso.Execute(20, 20);
        Level.CreateBorders();
        Level.Background.CreateGradient(Color.Orange, Color.Red);
    }

    /// <summary>
    /// Aliohjelma, joka luo uuden pelin kun olet kuollut 3. kertaa, läpäissyt kaikki tasot tai aloitat pelaamaan ensimmäistä kertaa. 
    /// </summary>
    private void UusiPeli()
    {
        PisteTilastot();
        ElamienMaara();
        nykyinenKentta = 0;
        valiAika = 0;
        LataaKenttaUudelleen();
        LuoAsePelaajalle();
        LuoPeliKello();
        elamaLaskuri.Value = 3;
        pisteLaskuri.Reset();
    }

    /// <summary>
    /// Aliohjelma joka luo aseen pelaajalle.
    /// </summary>
    private void LuoAsePelaajalle()
    {
        ase = new Cannon(30, 10);
        ase.InfiniteAmmo = true;
        ase.AttackSound = null;
        ase.AmmoIgnoresGravity = true;
        ase.ProjectileCollision = HeittoTahtiTormasiVihuun;
        pelaaja.Add(ase);
        ase.IsVisible = false;
    }
    /// <summary>
    /// Aliohjelma, jossa luodaan pelaajan ase heittotähdeksi, sekä asetetaan pelaaja heittämään heittotähtiä.
    /// </summary>
    /// <param name="hahmo">hahmo on tässä tapauksessa pelaaja itse</param>
    private void PelaajaHeita(PlatformCharacter hahmo)
    {
        PhysicsObject heittoTahti = ase.Shoot();
        if (heittoTahti != null)
        {
            heittoTahti.Image = heittoTahtiKuva;
        }
    }
    /// <summary>
    /// Aliohjelma, jossa ladataan kenttä uudelleen jos jokin tietyistä ehdoista toteutuu. Esimerkiksi kuollessa tai esteeseen törmätessä.
    /// </summary>
    private void LataaKenttaUudelleen()
    {
        tahtienLkmKentassa = 0;
        elamienLkm = elamaLaskuri.Value;
        if (restartDelay != null)
        {
            restartDelay.Stop();
        }

        if (nykyinenKentta == 3)
        {
            ajastin.Stop();
            valiAika = ajastin.CurrentTime;
            MessageDisplay.Add("Onneksi olkoon läpäisit kaikki tasot!");
            Timer.SingleShot(5, TopTenLista);
        }
        else
        {
            valiAika = ajastin.CurrentTime;
            keratytPisteet = pisteLaskuri.Value;
            ClearAll();
            LuoPeliKello();
            LuoKentta(kentat[nykyinenKentta]);
            LuoAsePelaajalle();
            LisaaNappaimet();
            Camera.StayInLevel = true;
            Camera.Follow(pelaaja);
            Camera.ZoomFactor = 1.2;
            Gravity = new Vector(0, -1500);
        }
    }
    /// <summary>
    /// Aliohjelma, jossa luodaan pelaaja peliin. Joka on tässä tapauksessa musta ninja.
    /// </summary>
    /// <param name="paikka">Pelaajan paikka kentässä</param>
    /// <param name="leveys">Pelaajan leveys</param>
    /// <param name="korkeus">Pelaajan korkeus</param>
    private void LisaaPelaaja(Vector paikka, double leveys, double korkeus)
    {
        pelaaja = new PlatformCharacter(leveys, korkeus);
        pelaaja.Position = paikka;
        pelaaja.Tag = "pelaaja";
        pelaaja.Image = pelaajanKuva;
        AddCollisionHandler(pelaaja, "tahti", PelaajaTormasiTahteen);
        AddCollisionHandler(pelaaja, "kruunu", PelaajaTormasiKruunuun);
        AddCollisionHandler(pelaaja, "este", PelaajaTormasiEsteeseen);
        ElamienMaara();
        Add(pelaaja);
    }
    /// <summary>
    /// Aliohjelma jossa pelaaja kerää tähden.
    /// </summary>
    /// <param name="pelaaja">Pelin pelaaja, mitä ohjataan</param>
    /// <param name="tahti">Pelissä kerättävät tähdet</param>
    private void PelaajaTormasiTahteen(PhysicsObject pelaaja, PhysicsObject tahti)
    {
        tahti.Destroy();
        tahtienLkmKentassa--;
        pisteLaskuri.Value += 50;
    }

    /// <summary>
    /// Aliohjelma, jossa pelaaja voi kerätä kruunun ja siirtyä seuraavaan kenttään vasta kun kaikki tähdet ovat kerätty.
    /// </summary>
    /// <param name="pelaaja">Pelin pelaaja, mitä ohjataan</param>
    /// <param name="kruunu">Pelissä kerättävät kruunut</param>
    private void PelaajaTormasiKruunuun(PhysicsObject pelaaja, PhysicsObject kruunu)
    {
        if (tahtienLkmKentassa == 0)
        {
            kruunu.Destroy();
            pisteLaskuri.Value += 100;
            SeuraavaKentta();
        }
    }
    /// <summary>
    /// Aliohjelma, jossa pelaaja törmää esteeseen ja sen seuraukset.
    /// </summary>
    /// <param name="pelaaja">Pelin pelaaja, mitä ohjataan</param>
    /// <param name="este">Este pelissä, johon osumalla kuolee</param>
    private void PelaajaTormasiEsteeseen(PhysicsObject pelaaja, PhysicsObject este)
    {
        pelaaja.Destroy();
        elamienLkm--;
        if (elamienLkm == 0)
        {
            elamaLaskuri.Value -= 1;
            ClearAll();
            MessageDisplay.Add("Hävisit pelin!");
            mainMenuDelay = Timer.CreateAndStart(3, MainMenu);
        }
        else
        {
            elamaLaskuri.Value -= 1;
            pisteLaskuri.Reset();
            MessageDisplay.Add("Kuolit!");
            restartDelay = Timer.CreateAndStart(1, LataaKenttaUudelleen);
        }
    }
    /// <summary>
    /// Aliohjelma, jossa luodaan peliin vihollisninja, vihollisen ase sekä ajastus heittotähtien heittämiselle.
    /// </summary>
    /// <param name="paikka">Vihollisen paikka</param>
    /// <param name="leveys">Vihollisen leveys</param>
    /// <param name="korkeus">Vihollisen korkeus</param>
    private void LisaaVihollinen(Vector paikka, double leveys, double korkeus)
    {
        PlatformCharacter vihollinen = new PlatformCharacter(leveys, korkeus);
        vihollinen.Position = paikka;
        vihollinen.Image = vihuKuva;
        vihollinen.Tag = "vihu";

        Cannon vihunAse = new Cannon(30, 10);
        vihunAse.InfiniteAmmo = true;
        vihunAse.AmmoIgnoresGravity = true;
        vihunAse.AttackSound = null;
        vihunAse.ProjectileCollision = HeittoTahtiTormasiPelaajaan;
        vihollinen.Add(vihunAse);
        vihunAse.IsVisible = false;


        Timer tahtienSylkyTimer = Timer.CreateAndStart(3, delegate { VihuHeita(vihunAse, vihollinen); });

        PlatformWandererBrain aivot = new PlatformWandererBrain();
        aivot.Speed = 20;
        vihollinen.Brain = aivot;
        Add(vihollinen);
    }


    /// <summary>
    /// Aliohjelma, jossa määräytyvät ehdot vihollisten heittotähtien heittämiselle. Esimerkiksi vihollisen mennessä vasemmalle, heitetään vasemmalle ja sama oikealle mentäessä, mutta silloin heitetään oikealle.
    /// </summary>
    /// <param name="vihunase">Vihollisen ase, eli heittotähti</param>
    /// <param name="vihollinen">Pelin vihollinen</param>
    private void VihuHeita(Cannon vihunase, PlatformCharacter vihollinen)
    {
        if (vihollinen.FacingDirection == Direction.Right)
        {
            vihunase.Angle = Angle.FromDegrees(0);
        }
        if (vihollinen.FacingDirection == Direction.Left)
        {
            vihunase.Angle = Angle.FromDegrees(180);
        }

        PhysicsObject heittoTahti = vihunase.Shoot();
        if (heittoTahti != null)
        {
            heittoTahti.Image = heittoTahtiKuva;
        }
    }
    /// <summary>
    /// Aliohjelma, jossa määritellään mitä tapahtuu kun pelaajan heittämä heittotähti osuu viholliseen
    /// </summary>
    /// <param name="heittotahti">Pelaajan ase millä heitetään vihollista</param>
    /// <param name="vihu">Pelin vihollinen</param>
    private void HeittoTahtiTormasiVihuun(PhysicsObject heittotahti, PhysicsObject vihu)
    {
        if (vihu.Tag.ToString() == "vihu")
        {
            vihu.Destroy();
            heittotahti.Destroy();
            MessageDisplay.Add("Tapoit vihun!");
        }
        if (vihu.Tag.ToString() != "tahti" && vihu.Tag.ToString() != "este" && vihu.Tag.ToString() != "kruunu")
        {
            heittotahti.Destroy();
        }
        

    }
    /// <summary>
    /// Aliohjelma, jossa määritellään mitä tapahtuu, kun vihollisen heittotähti osuu pelaajaan.
    /// </summary>
    /// <param name="heittotahti">Vihollisen ase</param>
    /// <param name="osuttu">Tässä tapauksessa pelaaja</param>
    private void HeittoTahtiTormasiPelaajaan(PhysicsObject heittotahti, PhysicsObject osuttu)
    {
        if (osuttu == pelaaja)
        {
            osuttu.Destroy();
            heittotahti.Destroy();
            elamienLkm--;
            if (elamienLkm == 0)
            {
                elamaLaskuri.Value -= 1;
                ClearAll();
                MessageDisplay.Add("Hävisit pelin!");
                mainMenuDelay = Timer.CreateAndStart(3, MainMenu);
            }
            else
            {
                elamaLaskuri.Value -= 1;
                pisteLaskuri.Reset();
                MessageDisplay.Add("Kuolit!");
                restartDelay = Timer.CreateAndStart(3, LataaKenttaUudelleen);
            }
        }
        if (osuttu.Tag.ToString() != "tahti" && osuttu.Tag.ToString() != "este" && osuttu.Tag.ToString() != "kruunu")
        {
            heittotahti.Destroy();
        }
    }
    /// <summary>
    /// Aliohjelma, jossa luodaan peliin kruunu
    /// </summary>
    /// <param name="paikka">Kruunun paikka</param>
    /// <param name="leveys">Kruunun leveys</param>
    /// <param name="korkeus">Kruunun korkeus</param>
    private void LisaaKruunu(Vector paikka, double leveys, double korkeus)
    {
        PhysicsObject kruunu = PhysicsObject.CreateStaticObject(leveys, korkeus);
        kruunu.IgnoresCollisionResponse = true;
        kruunu.Position = paikka;
        kruunu.Image = kruunuKuva;
        kruunu.Tag = "kruunu";
        Add(kruunu);
    }
    /// <summary>
    /// Alihojelma, jossa luodaan peliin tähti.
    /// </summary>
    /// <param name="paikka">Tähden paikka</param>
    /// <param name="leveys">Tähden leveys</param>
    /// <param name="korkeus">Tähden korkeus</param>
    private void LisaaTahti(Vector paikka, double leveys, double korkeus)
    {
        PhysicsObject tahti = PhysicsObject.CreateStaticObject(leveys, korkeus);
        tahti.IgnoresCollisionResponse = true;
        tahti.Position = paikka;
        tahti.Image = tahtiKuva;
        tahti.Tag = "tahti";
        Add(tahti);
        tahtienLkmKentassa++;
        PisteTilastot();
    }
    /// <summary>
    /// Aliohjelma, jossa luodaan peliin este.
    /// </summary>
    /// <param name="paikka">Esteen paikka</param>
    /// <param name="leveys">Esteen leveys</param>
    /// <param name="korkeus">Esteen korkeus</param>
    private void LisaaEste(Vector paikka, double leveys, double korkeus)
    {
        PhysicsObject este = PhysicsObject.CreateStaticObject(leveys, korkeus);
        este.IgnoresCollisionResponse = true;
        este.Position = paikka;
        este.Image = esteKuva;
        este.Tag = "este";
        Add(este);
    }
    /// <summary>
    /// Aliohjelma jossa luodaan kenttään taso, joita pitkin hypitään.
    /// </summary>
    /// <param name="paikka">Tason paikka</param>
    /// <param name="leveys">Tason leveys</param>
    /// <param name="korkeus">Tason korkeus</param>
    private void LisaaTaso(Vector paikka, double leveys, double korkeus)
    {
        PhysicsObject taso = PhysicsObject.CreateStaticObject(leveys, korkeus);
        taso.Position = paikka;
        taso.Color = Color.Gray;
        Add(taso);
    }
    /// <summary>
    /// Aliohjelma, jossa määritetään pelissä käytettävät näppäimet.
    /// </summary>
    private void LisaaNappaimet()
    {
        Keyboard.Listen(Key.F1, ButtonState.Pressed, ShowControlHelp, "Näytä ohjeet");
        Keyboard.Listen(Key.Escape, ButtonState.Pressed, ConfirmExit, "Lopeta peli");

        Keyboard.Listen(Key.Left, ButtonState.Down, LiikutaVasemmalle, "Liikkuu vasemmalle", pelaaja, -nopeus);
        Keyboard.Listen(Key.Right, ButtonState.Down, LiikutaOikealle, "Liikkuu oikealle", pelaaja, nopeus);
        Keyboard.Listen(Key.Up, ButtonState.Pressed, PelaajaHyppaa, "Pelaaja hyppää", pelaaja, hyppynopeus);
        Keyboard.Listen(Key.X, ButtonState.Pressed, PelaajaHeita, "Pelaaja heittää heittotähden", pelaaja);

        Keyboard.Listen(Key.Escape, ButtonState.Pressed, ConfirmExit, "Lopeta peli");

        ControllerOne.Listen(Button.DPadLeft, ButtonState.Down, LiikutaVasemmalle, "Pelaaja liikkuu vasemmalle", pelaaja, -nopeus);
        ControllerOne.Listen(Button.DPadRight, ButtonState.Down, LiikutaOikealle, "Pelaaja liikkuu oikealle", pelaaja, -nopeus);
        ControllerOne.Listen(Button.A, ButtonState.Pressed, PelaajaHyppaa, "Pelaaja hyppää", pelaaja, hyppynopeus);

        PhoneBackButton.Listen(ConfirmExit, "Lopeta peli");
    }
    /// <summary>
    /// Aliohjelma, jossa määritetään pelaaja liikkumaan oikealle sekä aseen kulma oikealle.
    /// </summary>
    /// <param name="hahmo">Pelin pelaaja</param>
    /// <param name="nopeus">Pelaajan kävelynopeus</param>
    private void LiikutaOikealle(PlatformCharacter hahmo, double nopeus)
    {
        ase.Angle = Angle.FromDegrees(0);
        hahmo.Walk(nopeus);
    }
    /// <summary>
    /// Aliohjelma, jossa määritetään pelaaja liikkumaan vasemmalle sekä aseen kulma vasemmalle.
    /// </summary>
    /// <param name="hahmo">Pelin pelaaja</param>
    /// <param name="nopeus">pelaajan kävelynopeus</param>
    private void LiikutaVasemmalle(PlatformCharacter hahmo, double nopeus)
    {
        ase.Angle = Angle.FromDegrees(180);
        hahmo.Walk(nopeus);
    }

    /// <summary>
    /// Aliohjelma, jossa määritellään pelaaja hyppäämään.
    /// </summary>
    /// <param name="hahmo">Pelin pelaaja</param>
    /// <param name="nopeus">Nopeus millä pelaaja hyppää</param>
    private void PelaajaHyppaa(PlatformCharacter hahmo, double nopeus)
    {
        hahmo.Jump(nopeus);
    }

}

