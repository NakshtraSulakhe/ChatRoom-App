const token = localStorage.getItem("jwtToken"); // Get JWT Token

const connection = new signalR.HubConnectionBuilder()
    .withUrl("/chathub", {
        accessTokenFactory: () => token // Send token for authentication
    })
    .configureLogging(signalR.LogLevel.Information)
    .build();

// Connect to SignalR
connection.start().catch(err => console.error(err));

// Listen for incoming messages
connection.on("ReceiveMessage", (user, message) => {
    const msg = document.createElement("li");
    msg.textContent = `${user}: ${message}`;
    document.getElementById("messagesList").appendChild(msg);
});

// Send a message when the button is clicked
document.getElementById("sendButton").addEventListener("click", () => {
    const message = document.getElementById("messageInput").value;
    connection.invoke("SendMessage", message).catch(err => console.error(err));
});
