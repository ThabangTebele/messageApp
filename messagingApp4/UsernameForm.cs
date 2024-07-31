using System;
using System.Windows.Forms;

namespace messagingApp4
{
    public partial class UsernameForm : Form
    {
        // Property to store the entered username
        public string Username { get; private set; }

        public UsernameForm()
        {
            InitializeComponent();
        }

        private void btnDone_Click(object sender, EventArgs e)
        {
            // Check if the username textbox is empty
            if (string.IsNullOrWhiteSpace(txtUsername.Text))
            {
                MessageBox.Show("Please enter a username.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
              
                return;
            }

            // Set the username to the value entered in the textbox
            Username = txtUsername.Text;

            // Close the form
            DialogResult = DialogResult.OK;
        }
    }
}
