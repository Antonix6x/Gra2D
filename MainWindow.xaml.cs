﻿using Microsoft.Win32;
using System;
using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace Gra2D
{
    public partial class MainWindow : Window
    {
        // Stałe reprezentujące rodzaje terenu
        public const int LAS = 1;     // las
        public const int LAKA = 2;     // łąka
        public const int SKALA = 3;   // skały
        public const int ZLOTO = 4;   //złoto
        public const int CHATKA = 5;    //chatka
        public const int ILE_TERENOW = 6;   // ile terenów
        // Mapa przechowywana jako tablica dwuwymiarowa int
        private int[,] mapa;
        private int szerokoscMapy;
        private int wysokoscMapy;
        // Dwuwymiarowa tablica kontrolek Image reprezentujących segmenty mapy
        private Image[,] tablicaTerenu;
        // Rozmiar jednego segmentu mapy w pikselach
        private const int RozmiarSegmentu = 32;

        // Tablica obrazków terenu – indeks odpowiada rodzajowi terenu
        // Indeks 1: las, 2: łąka, 3: skały
        private BitmapImage[] obrazyTerenu = new BitmapImage[ILE_TERENOW];

        // Pozycja gracza na mapie
        private int pozycjaGraczaX = 0;
        private int pozycjaGraczaY = 0;
        // Obrazek reprezentujący gracza
        private Image obrazGracza;
        // Licznik zgromadzonego drewna
        private int iloscDrewna = 0;
        private int iloscZlota = 0;
        private int iloscSkal = 0;
        private List<Misja> misje = new List<Misja>();
       private int indeksAktualnejMisji = 0;
        public OpenFileDialog oknoDialogowe = new OpenFileDialog();

        public MainWindow()
        {
            InitializeComponent();
            WczytajObrazyTerenu();

            MenuGlowne.Visibility = Visibility.Visible;
            SiatkaMapy.Visibility = Visibility.Collapsed;
            // Inicjalizacja obrazka gracza
            obrazGracza = new Image
            {
                Width = RozmiarSegmentu,
                Height = RozmiarSegmentu
            };
            BitmapImage bmpGracza = new BitmapImage(new Uri("gracz.png", UriKind.Relative));
            obrazGracza.Source = bmpGracza;
            

            misje.Add(new Misja { Nazwa="Zdobądź troche drewna i dotrzyj do chatki"});
            misje.Add(new Misja { Nazwa = "Zbuduj młot i dotrzyj do chatki"});
            misje.Add(new Misja { Nazwa = "Zbuduj złoty miecz i dotrzyj do chatki" });
        }
        private void WczytajObrazyTerenu()
        {
            // Zakładamy, że tablica jest indeksowana od 0, ale używamy indeksów 1-3
            obrazyTerenu[LAS] = new BitmapImage(new Uri("las.png", UriKind.Relative));
            obrazyTerenu[LAKA] = new BitmapImage(new Uri("laka.png", UriKind.Relative));
            obrazyTerenu[SKALA] = new BitmapImage(new Uri("skala.png", UriKind.Relative));
            obrazyTerenu[ZLOTO] = new BitmapImage(new Uri("zloto.png", UriKind.Relative));
            obrazyTerenu[CHATKA] = new BitmapImage(new Uri("chatka.png", UriKind.Relative));
        }

        // Wczytuje mapę z pliku tekstowego i dynamicznie tworzy tablicę kontrolek Image
        private void WczytajMape(string sciezkaPliku)
        {
            try
            {
                var linie = File.ReadAllLines(sciezkaPliku);//zwraca tablicę stringów, np. linie[0] to pierwsza linia pliku
                wysokoscMapy = linie.Length;
                szerokoscMapy = linie[0].Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;//zwraca liczbę elementów w tablicy
                mapa = new int[wysokoscMapy, szerokoscMapy];

                for (int y = 0; y < wysokoscMapy; y++)
                {
                    var czesci = linie[y].Split(' ', StringSplitOptions.RemoveEmptyEntries);//zwraca tablicę stringów np. czesci[0] to pierwszy element linii
                    for (int x = 0; x < szerokoscMapy; x++)
                    {
                        mapa[y, x] = int.Parse(czesci[x]);//wczytanie mapy z pliku
                    }
                }

                // Przygotowanie kontenera SiatkaMapy – czyszczenie elementów i definicji wierszy/kolumn
                SiatkaMapy.Children.Clear();
                SiatkaMapy.RowDefinitions.Clear();
                SiatkaMapy.ColumnDefinitions.Clear();

                for (int y = 0; y < wysokoscMapy; y++)
                {
                    SiatkaMapy.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(RozmiarSegmentu) });
                }
                for (int x = 0; x < szerokoscMapy; x++)
                {
                    SiatkaMapy.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(RozmiarSegmentu) });
                }

                // Tworzenie tablicy kontrolk Image i dodawanie ich do siatki
                tablicaTerenu = new Image[wysokoscMapy, szerokoscMapy];
                for (int y = 0; y < wysokoscMapy; y++)
                {
                    for (int x = 0; x < szerokoscMapy; x++)
                    {
                        Image obraz = new Image
                        {
                            Width = RozmiarSegmentu,
                            Height = RozmiarSegmentu
                        };

                        int rodzaj = mapa[y, x];
                        if (rodzaj >= 1 && rodzaj < ILE_TERENOW)
                        {
                            obraz.Source = obrazyTerenu[rodzaj];//wczytanie obrazka terenu
                        }
                        else
                        {
                            obraz.Source = null;
                        }

                        Grid.SetRow(obraz, y);
                        Grid.SetColumn(obraz, x);
                        SiatkaMapy.Children.Add(obraz);//dodanie obrazka do siatki na ekranie
                        tablicaTerenu[y, x] = obraz;
                    }
                }

                // Dodanie obrazka gracza – ustawiamy go na wierzchu
                SiatkaMapy.Children.Add(obrazGracza);
                Panel.SetZIndex(obrazGracza, 1);//ustawienie obrazka gracza na wierzchu
                pozycjaGraczaX = 0;
                pozycjaGraczaY = 0;
                AktualizujPozycjeGracza();

                iloscDrewna = 0;
                EtykietaDrewna.Content = "Drewno: " + iloscDrewna;
            }//koniec try
            catch (Exception ex)
            {
                MessageBox.Show("Błąd wczytywania mapy: " + ex.Message);
            }
        }

        // Aktualizuje pozycję obrazka gracza w siatce
        private void AktualizujPozycjeGracza()
        {
            Grid.SetRow(obrazGracza, pozycjaGraczaY);
            Grid.SetColumn(obrazGracza, pozycjaGraczaX);
        }

        // Obsługa naciśnięć klawiszy – ruch gracza oraz wycinanie lasu
        private void OknoGlowne_KeyDown(object sender, KeyEventArgs e)
        {
            int nowyX = pozycjaGraczaX;
            int nowyY = pozycjaGraczaY;

            //zmiana pozycji gracza
            if (e.Key == Key.Up) nowyY--;
            else if (e.Key == Key.Down) nowyY++;
            else if (e.Key == Key.Left) nowyX--;
            else if (e.Key == Key.Right) nowyX++;
            //Gracz nie może wyjść poza mapę
            
            if (nowyX >= 0 && nowyX < szerokoscMapy && nowyY >= 0 && nowyY < wysokoscMapy)

            // Gracz nie może wejść na pole ze skałami
            {

                if (mapa[nowyY, nowyX] != SKALA && mapa[nowyY, nowyX] != ZLOTO)
                {
                    pozycjaGraczaX = nowyX;
                    pozycjaGraczaY = nowyY;
                    AktualizujPozycjeGracza();
                }
                /* else if (oknoDialogowe.FileName != "misja1.txt" && mapa[nowyX, nowyY] == SKALA)
                 {
                     pozycjaGraczaX = nowyX;
                     pozycjaGraczaY = nowyY;
                     AktualizujPozycjeGracza();


                 }*/
                
                if (!string.IsNullOrEmpty(oknoDialogowe.FileName)
                    && oknoDialogowe.FileName != "misja1.txt"
                    && nowyX >= 0 && nowyX < mapa.GetLength(0)
                    && nowyY >= 0 && nowyY < mapa.GetLength(1)
                    && mapa[nowyX, nowyY] == SKALA && mapa[nowyY, nowyX] == ZLOTO)
                {
                    pozycjaGraczaX = nowyX;
                    pozycjaGraczaY = nowyY;
                    AktualizujPozycjeGracza();
                }
            }


            // Obsługa wycinania lasu – naciskamy klawisz C
            if (e.Key == Key.C)
            {
                int typTerenu = mapa[pozycjaGraczaY, pozycjaGraczaX];

                switch (typTerenu)
                {
                    case LAS:
                        mapa[pozycjaGraczaY, pozycjaGraczaX] = LAKA;
                        tablicaTerenu[pozycjaGraczaY, pozycjaGraczaX].Source = obrazyTerenu[LAKA];
                        iloscDrewna++;
                        break;
                    case SKALA:
                        mapa[pozycjaGraczaY, pozycjaGraczaX] = LAKA;
                        tablicaTerenu[pozycjaGraczaY, pozycjaGraczaX].Source = obrazyTerenu[LAKA];
                        iloscSkal++;
                        break;
                    case CHATKA:
                        mapa[pozycjaGraczaY, pozycjaGraczaX] = LAKA;
                        tablicaTerenu[pozycjaGraczaY, pozycjaGraczaX].Source = obrazyTerenu[LAKA];
                        if (e.Key == Key.C)
                        {
                            MessageBox.Show("Brawo ukończyłeś misje");

                            oknoDialogowe.Filter = "Plik mapy (*.txt)|*.txt";
                            oknoDialogowe.InitialDirectory = AppDomain.CurrentDomain.BaseDirectory; // Ustawienie katalogu początkowego
                            bool? czyOtwartoMape = oknoDialogowe.ShowDialog();
                            if (czyOtwartoMape == true)
                            {
                                WczytajMape(oknoDialogowe.FileName);
                            }
                            iloscZlota = 0;
                            iloscDrewna = 0;
                            iloscSkal = 0;

                        }
                        break;
                }
            }
            if (e.Key == Key.N)
            {
                int typTerenu = mapa[pozycjaGraczaY, pozycjaGraczaX];

                if (typTerenu == ZLOTO)
                {
                    mapa[pozycjaGraczaY, pozycjaGraczaX] = LAKA;
                    tablicaTerenu[pozycjaGraczaY, pozycjaGraczaX].Source = obrazyTerenu[LAKA];
                    iloscZlota++;
                }
                else if (typTerenu == SKALA)
                {
                    mapa[pozycjaGraczaY, pozycjaGraczaX] = LAKA;
                    tablicaTerenu[pozycjaGraczaY, pozycjaGraczaX].Source = obrazyTerenu[LAKA];
                    iloscSkal++;
                }
                                


                }

            EtykietaDrewna.Content = $"drewno:  {iloscDrewna} Skała:{iloscSkal} Złoto: {iloscZlota}";
        }

        // Obsługa przycisku "Wczytaj mapę"
        private void WczytajMape_Click(object sender, RoutedEventArgs e)
        {

            oknoDialogowe.Filter = "Plik mapy (*.txt)|*.txt";
            oknoDialogowe.InitialDirectory = AppDomain.CurrentDomain.BaseDirectory; // Ustawienie katalogu początkowego
            bool? czyOtwartoMape = oknoDialogowe.ShowDialog();
            if (czyOtwartoMape == true)
            {
                WczytajMape(oknoDialogowe.FileName);
            }
            iloscZlota = 0;
            iloscDrewna = 0;
            iloscSkal = 0;


            EtykietaDrewna.Content = $"Drewno: {iloscDrewna} skała: {iloscSkal} Złoto: {iloscZlota}";
        }

        /*private void ListaSkinow_SelectionChanged(object sender, SelectionChangedEventArgs e)//metoda na zmiane skinow
        {
            /*if (Skiny.SelectedItem is ComboBoxItem wybranySkin)
            {
                string Skin = wybranySkin.Tag.ToString();

                try
                {
                    BitmapImage bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.UriSource = new Uri("gracz.png", UriKind.Relative);
                    bitmap.EndInit();
                   
                   // obrazGracza.Source = bitmap;

                }
                catch (Exception ex)
                {
                    MessageBox.Show("Nie udało się załadować skina");
                }
            }
        }*/
        public class Misja
        {
            public string Nazwa { get; set; }
            public int Drewno { get; set; }
            public int Skala { get; set; }
            public int Zloto { get; set; }
           
            public bool Ukonczona { get; set; }

            public bool CzyUkonczona(int drewno, int skala, int zloto)
            {
                return drewno >= Drewno && skala >= Skala && zloto >= Zloto;
            }
        }
   

        private void crafting_Click(object sender, RoutedEventArgs e)
        {
            if (indeksAktualnejMisji < misje.Count)
            {
                var m = misje[indeksAktualnejMisji];
                if (!m.Ukonczona && m.CzyUkonczona(iloscDrewna, iloscSkal, iloscZlota))
                {
                    iloscDrewna -= m.Drewno;
                    iloscSkal -= m.Skala;
                    iloscZlota -= m.Zloto;


                    m.Ukonczona = true;
                    

                    //indeksAktualnejMisji++;
                    //PokazAktualnaMisje();

                    EtykietaDrewna.Content = $"Drewno:  {iloscDrewna} Skały: {iloscSkal} Złoto:  {iloscZlota}";
                }
                else
                {
                    MessageBox.Show("Nie masz wystarczających surowców.");
                }
            }
        }

        private void Zacznij_Click(object sender, RoutedEventArgs e)
        {
            MenuGlowne.Visibility = Visibility.Collapsed;
            SiatkaMapy.Visibility = Visibility.Visible;
            WczytajMapke.Visibility = Visibility.Visible;
            NastepnaMisja.Visibility = Visibility.Visible;
            obraz.Visibility = Visibility.Visible;
            Skin1.Visibility = Visibility.Visible;
            Skin2.Visibility = Visibility.Visible;
            Skin3.Visibility = Visibility.Visible;
            EtykietaDrewna.Visibility = Visibility.Visible;
            crafting.Visibility = Visibility.Visible;
            przepisy.Visibility = Visibility.Visible;
            celMisji.Visibility = Visibility.Visible;
        }

        private void Skin1_Click(object sender, RoutedEventArgs e)
        {
            BitmapImage bmpGracza = new BitmapImage(new Uri("gracz.png", UriKind.Relative));
            obrazGracza.Source = bmpGracza;
        }

        private void Skin2_Click(object sender, RoutedEventArgs e)
        {
            BitmapImage bmpGracza = new BitmapImage(new Uri("gracz2.png", UriKind.Relative));
            obrazGracza.Source = bmpGracza;
        }

        private void Skin3_Click(object sender, RoutedEventArgs e)
        {
            BitmapImage bmpGracza = new BitmapImage(new Uri("gracz3.png", UriKind.Relative));
            obrazGracza.Source = bmpGracza;
        }

        private void NastepnaMisja_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Misja pierwsza, misja druga, misja trzecia", MessageBoxButton.OKCancel.ToString());
            

            if (indeksAktualnejMisji < misje.Count - 1)
                {
                    indeksAktualnejMisji++;



                    MessageBox.Show("Wybierz kolejną mapę przypisaną do misji");

                    OpenFileDialog oknoDialogowe = new OpenFileDialog();
                    oknoDialogowe.Filter = "Plik mapy (*.txt)|*.txt";
                    oknoDialogowe.InitialDirectory = AppDomain.CurrentDomain.BaseDirectory;
                    
                    bool? czyOtwarto = oknoDialogowe.ShowDialog();

                    if (czyOtwarto == true)
                    {
                        WczytajMape(oknoDialogowe.FileName);
                    }
                    else
                    {
                        MessageBox.Show("to była ostatnia misja.");
                        Application.Current.Shutdown();
                    }
                }
        }
    

        private void Zasady_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Zasady gry: \n     -Dotrzyj do chatki i wciśnij `C` dokładnie tak samo jak przy zbieraniu kamienia; \n     -Zbieranie surowców: Zbieranie drewna `C`, zbieranie skał `N` ;\n      - Nie powiększaj okna gry;\n     - Żyuczymy udanej gry!:)        \n ");

        }

        private void przepisy_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Przepisy  \n  Młot:    3- drewna, 2-skala  \n  kilof:      3-drewna, 3-skala   \n  miecz:  2-drewna,3-skala/zloto");
        }

        private void celMisji_Click(object sender, RoutedEventArgs e)
        {
            string nazwaPliku = System.IO.Path.GetFileName(oknoDialogowe.FileName);
            if (nazwaPliku == "misja1.txt")
            {
                MessageBox.Show($"Cel misji:    {misje[1]}");
            }
            else if (nazwaPliku == "misja2.txt")
            {
                MessageBox.Show($"Cel misji:     {misje[2]}");
            }
            else if (nazwaPliku == "misja3.txt")
            {
                MessageBox.Show($"Cel misji:    {misje[3]}");
            }
        }
    }
}


