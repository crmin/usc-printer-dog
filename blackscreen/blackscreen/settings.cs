using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO.Ports;

namespace blackscreen
{
    public partial class Settings : Form
    {
        public Settings()
        {
            InitializeComponent();
        }

        private void Settings_Load(object sender, EventArgs e)
        {
            String[] port_list = SerialPort.GetPortNames();
            foreach (string port in port_list)
            {
                //this.serial_list_combo.Items.Add(new { Text = port, Value = port });
                this.serial_list_combo.Items.Add(port);
            }
            this.serial_list_combo.Text = port_list[0];
        }
    }
}
