using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Numerics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Nethereum.Hex.HexTypes;
using Nethereum.RPC.Eth.DTOs;
using Nethereum.Util;
using Nethereum.Web3;
using Nethereum.Web3.Accounts;
using Newtonsoft.Json;
using Nethereum.Hex.HexConvertors.Extensions;

namespace Gotchi_pet
{
    public partial class Form1 : Form
    {
        string privateKey;
        string erc721TokenContractAddress = "0x86935F11C86623deC8a25696E1C19a8659CbF95d";
        long secondsago = 0;
        Boolean isStop = false;
        List<aavegotchi> items = new List<aavegotchi>();
        public class aavegotchi
        {
            public Int64 ID { get; set; }
            public Int64 Haunt { get; set; }
            public Int64 BRarity { get; set; }
            public Int64 Diff { get; set; }
            public Int64 MRarity { get; set; }
            public string Trait { get; set; }
            public string Equipped { get; set; }
            public Int64 Equippedprice { get; set; }
            public Int64 Exp { get; set; }
            public Int64 Level { get; set; }
            public Int64 Kinship { get; set; }
            public decimal Price { get; set; }
            public decimal PR { get; set; }
        }
        void Delay(int delay)
        {
            while (delay > 0)
            {
                Thread.Sleep(TimeSpan.FromSeconds(1));
                delay--;
                if (isStop)
                    break;
            }
        }
        private static HttpClient httpClient = new HttpClient();
        public static async Task<string> GetWebContent(string url, string senddata = "")
        {
            // Thiết lập các Header nếu cần
            httpClient.DefaultRequestHeaders.Add("Accept", "text/html,application/json");
            try
            {
                // Thực hiện truy vấn GET
                HttpResponseMessage response;

                if (senddata == "")
                {
                    response = await httpClient.GetAsync(url).ConfigureAwait(false);
                }
                else
                {

                    var content = new StringContent(senddata, Encoding.UTF8, "application/json");
                    response = await httpClient.PostAsync(url, content).ConfigureAwait(false);
                }
                // Phát sinh Exception nếu mã trạng thái trả về là lỗi
                response.EnsureSuccessStatusCode();

                // Đọc nội dung content trả về - ĐỌC CHUỖI NỘI DUNG
                var htmltext = await response.Content.ReadAsStringAsync();
                return htmltext;

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return null;
            }
        }
        void log(string text)
        {
            Control.CheckForIllegalCrossThreadCalls = false;
            textBox1.AppendText(text + Environment.NewLine);
        }
        public Form1()
        {
            InitializeComponent();
            if (File.Exists("Privatekey.txt")) { privateKey = File.ReadAllText("Privatekey.txt"); } else
            {
                File.CreateText("Privatekey.txt");
                MessageBox.Show("pls copy privatekey"); System.Environment.Exit(1);
            }
               if (privateKey == "") { MessageBox.Show("pls copy privatekey"); System.Environment.Exit(1); }
            Task t = new Task(async () => {

                await checkpettimeAsync();
            });
            t.Start();
        }

        async Task checkpettimeAsync()
        {
            var account = new Account(privateKey, 137); //"+ account.Address +"
            string postData = "{\"query\":\"    { users(where:{id:\\\"" + account.Address.ToLower() + "\\\"}) \\n {gotchisOwned(first:1000) {id \\n lastInteracted}}} \",\"variables\":null}";
        startcheck:
            int delaytime = 999999;
            if (isStop == true) return;
            var htmltask = GetWebContent("https://api.thegraph.com/subgraphs/name/aavegotchi/aavegotchi-core-matic", postData);
            var responseString = htmltask.Result;
            var yy = JsonConvert.DeserializeObject<dynamic>(responseString);
            Newtonsoft.Json.Linq.JArray xjproperty = yy.data.users[0].gotchisOwned;
            var gotchiscount = xjproperty.Count;
            DateTime foo = DateTime.Now;

            for (var i = 0; i < gotchiscount; i++)
            {
                int lastInteracted = Convert.ToInt32(yy.data.users[0].gotchisOwned[i].lastInteracted);
                string id = yy.data.users[0].gotchisOwned[i].id;
                secondsago = (((DateTimeOffset)foo).ToUnixTimeSeconds() - lastInteracted); // 43200s = 12h
                if (secondsago > 43200)
                {
                    string text = "Pet me master - " + id; // https://aavegotchi.com/gotchi/8775";
                    log(text);
                    await petmeAsync(id);
                    delaytime = 300;
                }
                else
                {
                    var waittime = 43200 - Convert.ToInt32(secondsago);
                    string text = "Next pet in " + waittime / 60 + " minutes (" + id + ")";
                    //var xyz2 = GetWebContent(urlString + text);
                    log(text);
                    if (waittime > 3600)
                    {
                        if (waittime < delaytime) { delaytime = waittime; }
                    }
                    else
                    {
                        if (waittime > 600)
                        { if (delaytime > 600) { delaytime = 600; } }
                        else
                        {
                            if (waittime < delaytime) { delaytime = waittime; };
                        }

                    }


                }
            }
            log("Delay: " + delaytime + "s ( " + delaytime / 60 + " minutes)");
            Delay(delaytime);
            goto startcheck;
            //foreach (Newtonsoft.Json.Linq.JProperty jproperty in yy.data.users[0].gotchisOwned[0])
            //{ MessageBox.Show("jproperty.Name = "+ jproperty.Name); }




        }
        async Task petmeAsync(string id)
        {
            if (id == "")
            {

                textBox1.Text += Environment.NewLine + DateTime.Now.ToShortTimeString() + " - No id, fail to pet ";

                return;
            }

            var account = new Account(privateKey, 137);
            var web3 = new Web3(account, "https://rpc-mainnet.maticvigil.com/");
            var ABI = @"[{""inputs"": [{ ""internalType"": ""uint256[]"", ""name"": ""_tokenIds"", ""type"": ""uint256[]"" }],""name"": ""interact"",""outputs"": [],""stateMutability"": ""nonpayable"",""type"": ""function""}]";
            var contract = web3.Eth.GetContract(ABI, erc721TokenContractAddress);
            var caller = contract.GetFunction("interact");
            var bigid = Convert.ToInt32(id);
            var data = caller.GetData(new List<BigInteger> { bigid });

            var transactionInput = new TransactionInput
            {
                From = account.Address,
                To = erc721TokenContractAddress,
                Value = new HexBigInteger(0),
                Data = data,
                Gas = new HexBigInteger(50000),
                GasPrice = new HexBigInteger(5000000000)
            };

            var txnSigned = await web3.Eth.TransactionManager.SignTransactionAsync(transactionInput).ConfigureAwait(false);
            var txnHash = TransactionUtils.CalculateTransactionHash(txnSigned);
            var transactionHash = await web3.Eth.TransactionManager.SendTransactionAsync(transactionInput).ConfigureAwait(false);
            var _txnHash = "0x" + txnHash;
            if (transactionHash == _txnHash)
            {
                log("Transaction Hash: https://polygonscan.com/tx/" + transactionHash);
            }
            else
            {
                log("Not matching - fail");
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            this.Hide();
            notifyIcon1.Visible = true;
        }

        private void notifyIcon1_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            this.Show();
            this.WindowState = FormWindowState.Normal;
            notifyIcon1.Visible = true;
        }
    }
}
