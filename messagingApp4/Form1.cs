using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using FireSharp.Config;
using FireSharp.Response;
using FireSharp.Interfaces;
using Firebase.Database;
using Firebase.Database.Query;


namespace messagingApp4
{
    public partial class MessagingApp4 : Form
    {
        // Firebase client
        IFirebaseClient client;
        FirebaseClient firebaseClient;

        private string username;


        IFirebaseConfig config = new FirebaseConfig
        {
            AuthSecret = "HSjUFxzclUeTDcYOYFYncCqktB2xIAPbn1F9ZjqM",
            BasePath = "https://one-to-one-chat-d23ea-default-rtdb.firebaseio.com/"
        };

        public MessagingApp4()
        {
            InitializeComponent();
            client = new FireSharp.FirebaseClient(config);


            if (client != null)
            {
                lblConncetionStatus.Text = "YOU HAVE CONNECTED SUCCESSFULLY!";
            }
            
            firebaseClient = new FirebaseClient("https://one-to-one-chat-d23ea-default-rtdb.firebaseio.com/");      // Initialize Firebase client with your Firebase project URL

            lstConnectedClients.SelectedIndexChanged += lstConnectedClients_SelectedIndexChanged;
            if (!PromptForUsername())
            {
                // If the username form was exited without entering a username, exit the application
                Application.Exit();
                return;
            }      // Prompt the user to enter their username when the application starts
            FetchConnectedClients();        // Fetch connected clients from Firebase after initializing the Firebase client           
            ListenForMessages();
            
            
        }

        private void lstConnectedClients_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (lstConnectedClients.SelectedItem != null)
            {
                string selectedUser = lstConnectedClients.SelectedItem.ToString();
                // Start listening for private messages from the selected user
                ListenForPrivateMessages(selectedUser);
            }
        }

        // Define the message class to match the structure in Firebase
        public class Message
        {
            public string Id { get; set; }
            public string Text { get; set; }
            public string Username { get; set; }
            public DateTime Timestamp { get; set; }
        }

        private bool PromptForUsername()
        {
            // Prompt the user to enter their username when the application starts
            using (var usernameForm = new UsernameForm())
            {
                if (usernameForm.ShowDialog() == DialogResult.OK)
                {
                    // Get the entered username from the form
                    username = usernameForm.Username;
                    return true; // Return true if username was entered
                }
                else
                {
                    // If the user closes the form without entering a username, return false
                    return false;
                }
            }
        }


        private async Task SendMessageToFirebase(string message)
        {         
            var messageId = Guid.NewGuid().ToString();      // Create a unique key for the message

            // Construct the message object
            var messageObj = new Message
            {
                Id = messageId,
                Text = message,
                Username = username,
                Timestamp = DateTime.UtcNow
            };

            try
            {
                // Send the message to Firebase
                await firebaseClient.Child("Group_Messages").PostAsync(messageObj);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to send message: {ex.Message}");
            }
        }

        private async Task SendPrivateMessageToFirebase(string message, string recipientUsername)
        {
            // Check if recipient username is provided
            if (string.IsNullOrWhiteSpace(recipientUsername))
            {
                MessageBox.Show("Recipient username is required.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // Check if message is provided
            if (string.IsNullOrWhiteSpace(message))
            {
                MessageBox.Show("Please enter a message to send.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            var messageId = Guid.NewGuid().ToString(); // Create a unique key for the message

            // Construct the message object
            var messageObj = new Message
            {
                Id = messageId,
                Text = message,
                Username = username,
                Timestamp = DateTime.UtcNow
            };

            try
            {
                // Send the message to Firebase under the recipient's node
                await firebaseClient.Child("MessagesToUsers").Child(recipientUsername).PostAsync(messageObj);
                MessageBox.Show($"Message sent to {recipientUsername} successfully.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to send message: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }


        private void ListenForPrivateMessages(string recipientUsername)
        {
            // Listen for new messages in the recipient's inbox node
            firebaseClient.Child("MessagesToUsers").Child(recipientUsername).Child(username).AsObservable<Message>().Subscribe(change =>
            {
                if (change.EventType == Firebase.Database.Streaming.FirebaseEventType.InsertOrUpdate)
                {
                    // Get the new message
                    var newMessage = change.Object;

                    // Check if newMessage is not null
                    if (newMessage != null)
                    {
                        // Update the UI to display the new message
                        Task.Run(() =>
                        {
                            Invoke(new Action(() =>
                            {
                                // Display the message in the UI
                                lstMessages2.Items.Add($"{newMessage.Username}: {newMessage.Text} [{newMessage.Timestamp.ToLocalTime().ToString("HH:mm")}]");
                            }));
                        });
                    }
                }
            });
        }





        private void ListenForMessages()
        {
            // Listen for new messages in the Messages node of Firebase database
            firebaseClient.Child("Group_Messages").AsObservable<Message>().Subscribe(change =>
            {
                if (change.EventType == Firebase.Database.Streaming.FirebaseEventType.InsertOrUpdate)
                {
                    // Get the new message
                    var newMessage = change.Object;

                    // Check if newMessage is not null
                    if (newMessage != null)
                    {
                        // Update the UI to display the new message
                        Task.Run(() =>
                        {
                            Invoke(new Action(() =>
                            {
                                lstMessages.Items.Add($"{newMessage.Username}: {newMessage.Text}  [{ newMessage.Timestamp.ToLocalTime().ToString("HH:mm")}]");
                                lstGroupUsers.Items.Add($"{newMessage.Username}");
                                lstConnectedClients.Items.Add($"{newMessage.Username}");
                                
                            }));
                        });
                    }
                }
            });
        }

        private async void btnSend2_Click(object sender, EventArgs e)
        {
            // Get the message from the textbox
            string message = txtTypeText2.Text;
            string selectedUser = lstConnectedClients.SelectedItem.ToString();

            if (!string.IsNullOrWhiteSpace(message) && !string.IsNullOrWhiteSpace(selectedUser))
            {
                // Clear the textbox
                txtTypeText2.Clear();

                // Send the message to the selected user
                await SendPrivateMessageToFirebase(message, selectedUser);
            }
            else
            {
                MessageBox.Show("Please select a user and enter a message to send.");
            }
        }

        private void LoadConnectedClients(List<string> connectedClients)
        {
            Console.WriteLine("LoadConnectedClients method called.");       // Add debugging statement to check if the method is being called
            
            if (lstConnectedClients.InvokeRequired)
            {
                // If this method is called from a non-UI thread, marshal the call to the UI thread
                lstConnectedClients.Invoke(new Action<List<string>>(LoadConnectedClients), connectedClients);
                return;
            }
            
            lstConnectedClients.Items.Clear();

            // Populate the lstConnectedClients listbox with the usernames of connected clients
            foreach (var client in connectedClients)
            {
                //lstConnectedClients.Items.Add(client);

                // Add debugging statement to check each connected client
                Console.WriteLine($"Adding connected client: {client}");
                lstConnectedClients.Items.Add(client);
            }
        }

        private async void FetchConnectedClients()
        {
            // Fetch connected clients from Firebase
            var connectedClients = await FetchConnectedClientsFromFirebase();
            LoadConnectedClients(connectedClients);
        }

        private async Task<List<string>> FetchConnectedClientsFromFirebase()
        {
            // Fetch connected clients from Firebase
            var snapshot = await firebaseClient.Child("ConnectedClients").OnceAsync<string>();
            return snapshot.Select(c => c.Object).ToList();
        }

        private async Task<List<string>> GetConnectedClients()
        {
            // Fetch connected clients from Firebase
            var snapshot = await firebaseClient.Child("ConnectedClients").OnceAsync<string>();
            return snapshot.Select(c => c.Object).ToList();
        }

        private async void btnSend_Click_1(object sender, EventArgs e)
        {
            // Get the message from the textbox
            string message = txtTypeText.Text;

            if (!string.IsNullOrWhiteSpace(message))
            {
                // Clear the textbox
                txtTypeText.Clear();

                // Send the message to Firebase
                await SendMessageToFirebase(message);
            }
            else
            {
                MessageBox.Show("Please enter a message to send.");
            }
        }


        private async Task UpdateConnectedClients(bool isConnected)
        {
            try
            {
                if (isConnected)
                {
                    // Add the username of the connected client to the ConnectedClients node
                    await firebaseClient.Child("ConnectedClients").Child(username).PutAsync(true);
                }
                else
                {
                    // Remove the username of the disconnected client from the ConnectedClients node
                    await firebaseClient.Child("ConnectedClients").Child(username).DeleteAsync();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to update connected clients: {ex.Message}");
            }
        }

        private void MessagingApp4_FormClosing(object sender, FormClosingEventArgs e)
        {
            // Update the connected clients list when the form is closing (client disconnecting)
            UpdateConnectedClients(false).Wait();
        }

        private void MessagingApp4_Load(object sender, EventArgs e)
        {

        }
    }
}
