if ! ((test -e /etc/ssl/private/nginx-selfsigned.key) && (test -e /etc/ssl/certs/nginx-selfsigned.crt)); then
    echo "Self-signed x509 sertificate files not detected. Generating..."
    openssl req -x509 -nodes -days 36500 -newkey rsa:2048 -subj "/CN=US/C=US/L=San Fransisco" -keyout /etc/ssl/private/nginx-selfsigned.key -out /etc/ssl/certs/nginx-selfsigned.crt
fi



echo "Spinning up the Nginx reverse-proxy..."
nginx
echo "Spinning up the Wireguard service..."
wg-quick up wg0 && wg show wg0
echo "Spinning up the ASP WebAPI..."
dotnet /app/vdb_node_api.dll --no-launch-profile

tail -f /dev/null