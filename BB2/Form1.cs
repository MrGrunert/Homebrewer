using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using System.Xml.Linq;
using System.Xml.Serialization;

namespace BB2
{

    public partial class BB1_Main : Form
    {
        List<string> _maltsList = new List<string>();
        Dictionary<string, int> test = new Dictionary<string, int>();
        List<string> _sg = new List<string>();
        List<string> _hopsList = new List<string>();

        string _malt;
        string _hops;
        string _viktMalt;
        double _viktHumle;
        string _acid;
        decimal _attenuation;

        List<string> MaltNameList = new List<string>();
        List<string> HopsNameList = new List<string>();
        List<decimal> MaltPotentialList = new List<decimal>();
        Dictionary<string, decimal> expDic = new Dictionary<string, decimal>();


        public BB1_Main()
        {
            InitializeComponent();
            initListView();
            // PopulateLists();
            LoadXMLData();
            // CalculateIBU();

            calculateOgButton.Hide();
            loadDocButton.Hide();
            // saveDataButton.Hide();
            loadPotentialDocBtn.Hide(); //denna bör byta namn till IBU och det skall läggas till visning för ibu. 
        }

        private void initListView()
        {
            MaltListView.View = View.Details;
            MaltListView.Columns.Add("Malt");
            MaltListView.Columns.Add("Vikt kg");
            MaltListView.Columns.Add("Pot");

            hopsListView.View = View.Details;
            hopsListView.Columns.Add("Humle");
            hopsListView.Columns.Add("Vikt g");
            hopsListView.Columns.Add("Syra %");
            hopsListView.Columns.Add("Tid");
            //hopsListView.Columns.Add("IBU");

            predictedValuesListView.View = View.Details;
            predictedValuesListView.Columns.Add("Uppskattade värden");
            predictedValuesListView.Columns.Add("OG");
            predictedValuesListView.Columns.Add("FG");
            predictedValuesListView.Columns.Add("ABV");


        }

        private void AddMalt_Click(object sender, EventArgs e)
        {
            if (!string.IsNullOrEmpty(maltKgTextbox.Text))
            {
                string kilogram = maltKgTextbox.Text;

                if (kilogram.Contains('.'))
                {
                    kilogram = kilogram.Replace('.', ',');
                }

                _malt = maltListBox.SelectedItem.ToString();
                _viktMalt = kilogram;

                var item1 = new ListViewItem(new[] { _malt, _viktMalt, pointLabel.Text, });

                MaltListView.Items.Add(item1);
            }
            else
            {
                MessageBox.Show("Fyll i kg malt.");
            }
        }


        private void addHopsButton_Click(object sender, EventArgs e)
        {
            _hops = hopsListBox.SelectedItem.ToString();
            if (!string.IsNullOrEmpty(gramsTextBox.Text) && !string.IsNullOrEmpty(alphaAcidTextBox.Text) && !string.IsNullOrEmpty(timeHopsTextBox.Text))
            {
                _viktHumle = double.Parse(gramsTextBox.Text);
                _acid = alphaAcidTextBox.Text;
                if (_acid.Contains('.'))
                {
                    _acid = _acid.Replace('.', ',');
                }
                string time = timeHopsTextBox.Text;

                var item1 = new ListViewItem(new[] { _hops, _viktHumle.ToString(), _acid, time, "N/A" });

                hopsListView.Items.Add(item1);
            }
            else
            {
                MessageBox.Show("Fyll i alla humlefält");
            }

        }


        private void CalculateIBU()
        {
            string[] alphaAcids = hopsListView.Items.Cast<ListViewItem>().Select(x => x.SubItems[2].Text).ToArray();
            string[] hopsBoilTime = hopsListView.Items.Cast<ListViewItem>().Select(x => x.SubItems[3].Text).ToArray();
            string[] predictedOg = predictedValuesListView.Items.Cast<ListViewItem>().Select(x => x.SubItems[1].Text).ToArray();

            double alpha = double.Parse(alphaAcids.FirstOrDefault());
            double amount = _viktHumle;
            double finalVolume = double.Parse(afterBoilTextBox.Text);

            alpha = alpha / 100 + 1;
            var cAlpha = (alpha * amount * 10) / finalVolume;

            var boilGravity = double.Parse(predictedOg.FirstOrDefault());
            boilGravity = boilGravity / 1000 + 1;

            var gravityFactor = 1.65 * Math.Pow(0.000125, boilGravity - 1.0); //Bigness factor

            var time = double.Parse(hopsBoilTime.FirstOrDefault());
            var timeFactor = (1 - Math.Exp(-0.04 * time)) / 4.15; //boil time factort

            var hopUti = gravityFactor * timeFactor; //das ist ok....
            var hopUtilz = Math.Round(hopUti, 3);

            var result = cAlpha * hopUtilz;
        }


        private void calculateOgButton_Click(object sender, EventArgs e)
        {
            if (!string.IsNullOrEmpty(afterBoilTextBox.Text))
            {
                decimal afterBoli = decimal.Parse(afterBoilTextBox.Text); // mäsk efter kok 
                decimal mashInGallon = afterBoli * 0.2641720524M; //mäsk omräknat till gallon

                string[] maltListViewItems = MaltListView.Items.Cast<ListViewItem>().Select(x => x.SubItems[0].Text).ToArray();
                List<decimal> maltPotencialList = new List<decimal>();
                foreach (var item in maltListViewItems)
                {
                    decimal pot = expDic.Where(x => x.Key == item).Select(y => y.Value).SingleOrDefault();
                    var add = (pot * 1000) - 1000;
                    maltPotencialList.Add(add);
                }

                string[] maltInKiloGramList = MaltListView.Items.Cast<ListViewItem>().Select(item => item.SubItems[1].Text).ToArray();  //hämta malt i kg som string
                List<decimal> maltsInKGList = new List<decimal>(); // malt i kg som decimal
                foreach (var item in maltInKiloGramList)
                {
                    decimal maltInKG = decimal.Parse(item);
                    maltsInKGList.Add(maltInKG);
                }

                List<decimal> maltsInLBSList = new List<decimal>();
                foreach (var item in maltsInKGList)
                {
                    decimal maltinLBS = item * 2.2046226218M;
                    maltsInLBSList.Add(maltinLBS);
                }

                List<decimal> maltPotencialFinal = new List<decimal>();
                for (int i = 0; i < maltPotencialList.Count; i++)
                {
                    var val = maltPotencialList[i] * maltsInLBSList[i];
                    maltPotencialFinal.Add(val);
                }

                decimal totalMaltBill = maltPotencialFinal.Sum();
                decimal efficiency = 0.73M;
                decimal OG = (totalMaltBill * efficiency) / mashInGallon; //efficiency can be changed, depending on equipment... 
                decimal roundedOG = (int)Math.Floor(OG);
                decimal yeast = (100 - _attenuation) / 100; //0.25M; Yeast with 75% Attenuation  
                decimal FG = roundedOG * yeast;
                int roundedFG = (int)Math.Floor(FG);
                decimal ABV = (roundedOG - roundedFG) * 131.25M / 1000;

                predictedValuesListView.Items.Clear();
                var item1 = new ListViewItem(new[] { "  ", roundedOG.ToString(), roundedFG.ToString(), ABV.ToString() });

                predictedValuesListView.Items.Add(item1);
            }
            else
            {
                MessageBox.Show("Du måste fylla i fältet Efter kok");
            }

        }

        private void addYeastButton_Click(object sender, EventArgs e)
        {
            if (!string.IsNullOrEmpty(attenutaionTextBox.Text))
            {
                _attenuation = decimal.Parse(attenutaionTextBox.Text);
                yeast_Label.Text = $"{yeastSortTextBox.Text} - {_attenuation}%";
            }
            else
            {
                MessageBox.Show("Du måste fylla i jäst fält.");
            }

            if (MaltListView.Items.Count > 0 && MaltListView != null && hopsListView.Items.Count > 0 && hopsListView != null && _attenuation > 0)
            {
                calculateOgButton.Show();
                calOGLabel.Hide();
            }
        }

        private void maltDeleteButton_Click(object sender, EventArgs e)
        {
            if (MaltListView.SelectedItems.Count > 0)
            {
                var confirmation = MessageBox.Show("Vill du radera malt?", "Ta bort", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (confirmation == DialogResult.Yes)
                {
                    for (int i = MaltListView.Items.Count - 1; i >= 0; i--)
                    {
                        if (MaltListView.Items[i].Selected)
                        {
                            MaltListView.Items[i].Remove();
                        }

                    }
                }
                else
                    MessageBox.Show("Inget är borttaget");
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (hopsListView.SelectedItems.Count > 0)
            {
                var confirmation = MessageBox.Show("Vill du radera humle?", "Ta bort", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (confirmation == DialogResult.Yes)
                {
                    for (int i = hopsListView.Items.Count - 1; i >= 0; i--)
                    {
                        if (hopsListView.Items[i].Selected)
                        {
                            hopsListView.Items[i].Remove();
                        }

                    }
                }
                else
                    MessageBox.Show("Inget är borttaget");
            }
        }

        private void SaveData()
        {
            //exportData ex = new exportData();
            //XmlSerializer serializer = new XmlSerializer(typeof(item[]), new XmlRootAttribute() { ElementName = "items" });

            //using (FileStream fs = new FileStream("Data.xml", FileMode.Create))
            //{
            //    serializer.Serialize(fs,
            //     expDic.Select(kv => new item() { key = kv.Key, value = kv.Value }).ToArray());
            //}
        }

        //private void SaveHopsToXML()
        //{

        //   // List<string> hopsLines = new List<string>();
        //    using (StreamReader reader = new StreamReader("Humle.txt"))
        //    {
        //        string hopLine;

        //        while ((hopLine = reader.ReadLine()) != null)
        //        {
        //            HopsNameList.Add(hopLine);

        //            hopsListBox.Items.Add(hopLine);
        //        }
        //    }


        //    var serializer = new XmlSerializer(typeof(List<string>));
        //    using (var stream = File.OpenWrite("Humle.xml"))
        //    {
        //        serializer.Serialize(stream, HopsNameList);
        //    }

        //}

        //private void SaveMaltsToXML()
        //{
        //    List<string> maltLines = new List<string>();
        //    using (StreamReader r = new StreamReader("Malts.txt"))
        //    {
        //        string MaltLine;

        //        while ((MaltLine = r.ReadLine()) != null)
        //        {
        //            MaltNameList.Add(MaltLine);

        //            maltListBox.Items.Add(MaltLine);
        //        }
        //    }


        //    List<string> potentialLines = new List<string>();
        //    using (StreamReader r2 = new StreamReader("MaltPotential.txt"))
        //    {
        //        string potentialLine;
        //        string rightLine = "";

        //        while ((potentialLine = r2.ReadLine()) != null)
        //        {
        //            if (potentialLine == "0")
        //            {
        //                potentialLine = "0.0";
        //            }
        //            if (potentialLine == "1")
        //            {
        //                potentialLine = "1.0";
        //            }
        //            if (potentialLine.Contains('.'))
        //            {
        //                rightLine = potentialLine.Replace('.', ',');
        //            }

        //            decimal toAdd = decimal.Parse(rightLine);

        //            MaltPotentialList.Add(toAdd);
        //        }
        //    }


        //    var key = MaltNameList;
        //    var value = MaltPotentialList;

        //    var dic = key.Zip(value, (k, v) => new { k, v }).ToDictionary(x => x.k, x => x.v);

        //    expDic = dic;


        //    //exportData ex = new exportData();
        //    XmlSerializer serializer = new XmlSerializer(typeof(item[]), new XmlRootAttribute() { ElementName = "items" });

        //    using (FileStream fs = new FileStream("Data.xml", FileMode.Create))
        //    {
        //        serializer.Serialize(fs,
        //         expDic.Select(kv => new item() { key = kv.Key, value = kv.Value }).ToArray());


        //    }
        //}

        private void saveDataButton_Click(object sender, EventArgs e)
        {
            SaveBrewToTXT();
            //SaveHopsToXML();
            //SaveMaltsToXML();
            // SaveData();
        }

        private void SaveBrewToTXT()
        {
            DateTime time = DateTime.Now;
            string format = "ddd-d-MMM-HHmm-yyyy_";
            var brewDate = time.ToString(format);
            string brew = $"{brewDate}BrewData.txt";

            TextWriter txt = new StreamWriter(brew);

            string[] maltListViewItems = MaltListView.Items.Cast<ListViewItem>().Select(x => x.SubItems[0].Text).ToArray();
            string[] maltListViewItems2 = MaltListView.Items.Cast<ListViewItem>().Select(x => x.SubItems[1].Text).ToArray();
            string[] hopsListViewItem = hopsListView.Items.Cast<ListViewItem>().Select(x => x.SubItems[0].Text).ToArray();
            string[] hopsListViewItem2 = hopsListView.Items.Cast<ListViewItem>().Select(x => x.SubItems[1].Text).ToArray();
            string[] hopsListViewItem3 = hopsListView.Items.Cast<ListViewItem>().Select(x => x.SubItems[2].Text).ToArray();
            string[] hopsListViewItem4 = hopsListView.Items.Cast<ListViewItem>().Select(x => x.SubItems[3].Text).ToArray();

            txt.WriteLine("---MALT---");
            for (int i = 0; i < maltListViewItems.Length; i++)
            {
                string item = maltListViewItems[i];
                string item2 = maltListViewItems2[i];
                txt.WriteLine($"{item} - {item2} Kg");
            }

            txt.WriteLine(" ");
            txt.WriteLine("---HUMLE---");
            for (int i = 0; i < hopsListViewItem.Length; i++)
            {
                string item = hopsListViewItem[i];
                string item2 = hopsListViewItem2[i];
                string item3 = hopsListViewItem3[i];
                string item4 = hopsListViewItem4[i];
                txt.WriteLine($"{item} - {item2} Gram - {item3}% - {item4} Min");
            }

            txt.WriteLine(" ");
            txt.WriteLine($"Mäsk liter {mashTextBox.Text}");
            txt.WriteLine($"Lak liter {lakTextBox.Text}");
            txt.WriteLine($"Före kok liter {preBoilTextBox.Text}");
            txt.WriteLine($"Efter kok liter {afterBoilTextBox.Text}");
            txt.WriteLine($"Liter i jäskärl {fermentationTextBox.Text}");
            txt.WriteLine(" ");
            txt.WriteLine($"Mäskning Tid: {mashTimeTextBox.Text} Temp: {mashTempTextBox.Text}");
            txt.WriteLine($"Utmäskning Tid: {outMashTimeTextBox.Text} Temp: {outMashTempTextBox.Text}");
            txt.WriteLine($"Kok Tid: {boilTimeTextBox.Text} Temp: {boilTempTextBox.Text}");
            txt.WriteLine($"Primärjäsning Tid: {primaryFermTextBox.Text} Temp: {primaryFermTempTextBox.Text}");
            txt.WriteLine($"Sekundärjäsning Tid: {secFermTimeTextBox.Text} Temp: {secFermTempTextBox.Text}");
            txt.WriteLine(" ");

            txt.WriteLine($"Jästsortd & Attenuation: {yeast_Label.Text} ");
            txt.WriteLine(" ");

            var predictedOG = predictedValuesListView.Items.Cast<ListViewItem>().Select(x => x.SubItems[1].Text).ToArray();
            var predictedFG = predictedValuesListView.Items.Cast<ListViewItem>().Select(x => x.SubItems[2].Text).ToArray();
            var predictedABV = predictedValuesListView.Items.Cast<ListViewItem>().Select(x => x.SubItems[3].Text).ToArray(); ;

            txt.WriteLine($"Teoretiska värden OG:{predictedOG.FirstOrDefault()} FG:{predictedFG.FirstOrDefault()} ABV:{predictedABV.FirstOrDefault()}");

            txt.Close();
            MessageBox.Show($"Sparat som data som: {brew}");
        }

        private void LoadXMLData()
        {
            XmlSerializer serializer = new XmlSerializer(typeof(item[]),
                                new XmlRootAttribute() { ElementName = "items" });

            using (FileStream fs = new FileStream("Data.xml", FileMode.Open))
            {
                expDic = ((item[])serializer.Deserialize(fs))
               .ToDictionary(i => i.key, i => i.value);
            }

            maltListBox.DataSource = expDic.Keys.OrderBy(x => x).ToList();

            serializer = new XmlSerializer(typeof(List<string>));
            using (var stream = File.OpenRead("Humle.xml"))
            {
                _hopsList = (List<string>)(serializer.Deserialize(stream));

            }
            hopsListBox.DataSource = _hopsList.OrderBy(x => x).ToList();
        }

        private void loadXMLDataButton_Click(object sender, EventArgs e)
        {
            //XmlSerializer serializer = new XmlSerializer(typeof(item[]),
            //                    new XmlRootAttribute() { ElementName = "items" });

            //using (FileStream fs = new FileStream("Data.xml", FileMode.Open))
            //{
            //    expDic = ((item[])serializer.Deserialize(fs))
            //   .ToDictionary(i => i.key, i => i.value);
            //}

            //maltListBox.DataSource = expDic.Keys.ToList();

            //serializer = new XmlSerializer(typeof(List<string>));
            //using (var stream = File.OpenRead("Humle.xml"))
            //{
            //    _hopsList = (List<string>)(serializer.Deserialize(stream));

            //}
            //hopsListBox.DataSource = _hopsList;
            MessageBox.Show("Nope, gör inte mycket i nuläget! :D");
        }

        private void loadDocButton_Click(object sender, EventArgs e)
        {
            //List<string> hopsLines = new List<string>();
            //using (StreamReader reader = new StreamReader("Humle.txt"))
            //{
            //    string hopLine;

            //    while ((hopLine = reader.ReadLine()) != null)
            //    {
            //        HopsNameList.Add(hopLine);

            //        hopsListBox.Items.Add(hopLine);
            //    }
            //}

            //List<string> lines = new List<string>();
            //using (StreamReader r = new StreamReader("Malts.txt"))
            //{
            //    string line;

            //    while ((line = r.ReadLine()) != null)
            //    {
            //        MaltNameList.Add(line);

            //        maltListBox.Items.Add(line);
            //    }
            //}
        }

        private void loadPotentialDocBtn_Click(object sender, EventArgs e)
        {
            CalculateIBU();
            //List<string> lines = new List<string>();
            //using (StreamReader r = new StreamReader("MaltPotential.txt"))
            //{
            //    string line;
            //    string rightLine = "";

            //    while ((line = r.ReadLine()) != null)
            //    {
            //        if (line == "0")
            //        {
            //            line = "0.0";
            //        }
            //        if (line == "1")
            //        {
            //            line = "1.0";
            //        }
            //        if (line.Contains('.'))
            //        {
            //            rightLine = line.Replace('.', ',');
            //        }

            //        decimal toAdd = decimal.Parse(rightLine);

            //        MaltPotentialList.Add(toAdd);

            //        hopsListBox.Items.Add(toAdd);

            //    }
            //}
        }


        private void maltListBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            int index = maltListBox.SelectedIndex;
            string see = maltListBox.Items[index].ToString();
            decimal p = expDic.FirstOrDefault(x => x.Key == see).Value;
            string show = decimal.Round(p, 3).ToString();
            pointLabel.Text = show;
        }
    }
}
