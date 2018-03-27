using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Management;
using Newtonsoft.Json.Linq;

namespace blackscreen
{
    public partial class main_screen : Form
    {
        // ## Constants and Variables ##
        private string FEE_CHECK_API_URL = "https://usc.unist.in/api/fee/check";
        //private string FEE_CHECK_API_URL = "http://localhost:8080/api/fee";
        private string PRINT_API_URL = "https://usc.unist.in/api/fee/check";

        static HttpClient client = new HttpClient();

        private bool alt_f4_pressed = false;
        private long tick_cnt = 0;
        private long last_rfid_tick = 0;
        private string last_rfid_name = "";
        private string last_rfid_student_id = "R";
        private string last_before_rfid_student_id = "R";
        private bool fee_paid = false;
        private string print_str = "R";
        private int last_print_id = 0;

        // ## Private Methods ##
        private static DateTime Delay(int MS)
        {
            DateTime ThisMoment = DateTime.Now;
            TimeSpan duration = new TimeSpan(0, 0, 0, 0, MS);
            DateTime AfterWards = ThisMoment.Add(duration);
            while (AfterWards >= ThisMoment)
            {
                System.Windows.Forms.Application.DoEvents();
                ThisMoment = DateTime.Now;
            }
            return DateTime.Now;
        }

        private bool is_paid(string student_id)
        {
            /*
             * Rese API 호출을 통해 학생회비 납부 여부를 체크함
             * 납부 확인이 되면 true, 납부하지 않았거나 호출에 실패하는 경우 false 반환
             * 호출 형태는 [API_ROOT_END_POINT]/fee/check?number=20151683 꼴로 하도록 함
             * 반환 예시는 아래와 같음
             *      {
             *          "result": 0,
             *          "is_paid": true
             *      }
             */
            HttpClient client = new HttpClient();
            client.BaseAddress = new Uri(FEE_CHECK_API_URL);

            // Add an Accept header for JSON format.
            client.DefaultRequestHeaders.Accept.Add(
            new MediaTypeWithQualityHeaderValue("application/json"));

            // List data response.
            HttpResponseMessage response = client.GetAsync("?number=" + student_id).Result;  // Blocking call!
            if (response.IsSuccessStatusCode)
            {
                // Parse the response body. Blocking!
                //var dataObjects = response.Content.ReadAsAsync<IEnumerable<DataObject>>().Result;
                string dataStr = response.Content.ReadAsStringAsync().Result.ToString();
                JObject responseJson = JObject.Parse(dataStr);
                if (responseJson["result"].ToString().Equals("0")) // success
                {
                    last_rfid_name = responseJson["name"].ToString();
                    return Convert.ToBoolean(responseJson["fee"].ToString());
                }
            }
            return false;
        }

        private void print_event_send(string student_id, int page_number)
        {
            /*
             * Print Event가 캡쳐되면 호출
             * API End Point로 GET 요청을 보냄
             * 호출 형태는 [API_ROOT_END_POINT]/print?number=00000000&page=10 꼴로 하도록 함
             */
            HttpClient client = new HttpClient();
            client.BaseAddress = new Uri(PRINT_API_URL);

            // Add an Accept header for JSON format.
            client.DefaultRequestHeaders.Accept.Add(
            new MediaTypeWithQualityHeaderValue("application/json"));

            // List data response.
            HttpResponseMessage response = client.GetAsync("?number=" + student_id + "&page=" + page_number).Result;  // Blocking call!
            /*
            if (response.IsSuccessStatusCode)
            {
                string dataStr = response.Content.ReadAsStringAsync().Result.ToString();
                JObject responseJson = JObject.Parse(dataStr);
                if (responseJson["result"].ToString().Equals("0")) // success
                {
                    last_rfid_name = responseJson["name"].ToString();
                    return Convert.ToBoolean(responseJson["fee"].ToString());
                }
            }
            */
        }

        private static string GetPrintJobs()
        {
            string searchQuery = "SELECT * FROM Win32_PrintJob";
            ManagementObjectSearcher searchPrintJobs = new ManagementObjectSearcher(searchQuery);
            ManagementObjectCollection prntJobCollection = searchPrintJobs.Get();
            foreach (ManagementObject prntJob in prntJobCollection)
            {
                try
                {
                    string document = prntJob.Properties["Document"].Value.ToString();
                    string color = prntJob.Properties["Color"].Value.ToString();
                    string host = prntJob.Properties["HostPrintQueue"].Value.ToString();
                    string owner = prntJob.Properties["Owner"].Value.ToString();
                    string id = prntJob.Properties["JobId"].Value.ToString();
                    string pages = prntJob.Properties["TotalPages"].Value.ToString();

                    //Console.WriteLine("{0} Document '{1}', pages {2} {3}, sent by {4}\\{5}", id, document, pages, color, host, owner);
                    return id+'|'+pages;
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Exception getting print jobs: " + ex);
                }
            }
            return "|";
        }

        private int ParsePrintId(string print_job)
        {
            return int.Parse(print_job.Split('|')[0]);
        }

        private int ParsePrintPage(string print_job)
        {
            return int.Parse(print_job.Split('|')[1]);
        }


        // ## Event Processing ##
        public main_screen()
        {
            InitializeComponent();
        }

        private void main_screen_Load(object sender, EventArgs e)
        {
            this.rfid_serial.Open();
        }

        private void main_screen_KeyDown(object sender, KeyEventArgs e)
        {
            this.alt_f4_pressed = false;

            if (e.KeyCode == Keys.Q && e.Shift && e.Control)
                this.Close();
            else if (e.KeyCode == Keys.F4 && e.Alt)
                this.alt_f4_pressed = true;
        }

        private void main_screen_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (this.alt_f4_pressed)
            {
                e.Cancel = true;
                return;
            }
        }

        private void main_screen_FormClosed(object sender, FormClosedEventArgs e)
        {
            this.rfid_serial.Close();
        }

        private void id_reader_Tick(object sender, EventArgs e)
        {
            tick_cnt += 1;
            if (last_rfid_student_id.Equals("R"))
            {
                if(tick_cnt - last_rfid_tick >= 10)  // 3sec. (30*tick_unit(=1tick/100msec) sec.)
                {
                    this.student_id_label.Text = "Plase contact your ID card";
                    //this.BackColor = Color.Black;
                    this.TopMost = true;
                    this.WindowState = FormWindowState.Maximized;  // 최대화
                }
            }
            else
            {
                this.student_id_label.Text = "Student ID: " + print_str;
                if (last_rfid_student_id.Equals(last_before_rfid_student_id))
                {
                    if (fee_paid)  // 학생회비를 납부한 상태라면
                    {
                        // TODO: 화면 잠금 풀기 -> 이건 어떻게 할지 생각해보자 아마 TopMost=false 하고 최소화 하도록 하면 되지 않을까
                        print_str = last_rfid_student_id + "\n안녕하세요, " + last_rfid_name + "님\nHello "+ last_rfid_name;

                        if (tick_cnt - last_rfid_tick >= 10)
                        {
                            this.TopMost = false;
                            //this.BackColor = Color.Blue;
                            this.WindowState = FormWindowState.Minimized;  // 최소화
                        }
                    }
                    else
                    {
                        print_str = last_rfid_student_id + "\n학생회비를 납부하지 않으셨습니다\nYou did not paid student dues";
                    }
                }
                else
                {
                    fee_paid = is_paid(last_rfid_student_id);
                }
                last_before_rfid_student_id = last_rfid_student_id;
            }
            int print_job = GetPrintJobs();

        }

        private void rfid_serial_DataReceived(object sender, System.IO.Ports.SerialDataReceivedEventArgs e)
        {
            last_rfid_tick = tick_cnt;
            last_rfid_student_id = this.rfid_serial.ReadLine().Trim();
            print_str = last_rfid_student_id;
        }
    }
}
