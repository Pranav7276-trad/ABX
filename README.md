1. Clone the repo
2. In your machine open CMD and type ipconfig to find Local host IP address -> Under heading "Wireless LAN adapter Wi-Fi:" Search for "IPv4 Address" -> Copy the value present for this heading
3. Open the cloned program in Visual studio code. -> Goto program.cs -> On line no. 13 - "var client = new ABXClient("xxxx", 3000);" Change xxxx with the value copied under "IPv4 Address".
4. Click on run or Right-Click on ABX in Solution Explorer -> Open in Terminal -> type dotnet run
