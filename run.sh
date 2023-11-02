#!/bin/bash

# Share Wifi with Eth device
#
#
# This script is created to work with Raspbian Stretch
# but it can be used with most of the distributions
# by making few changes.
#
# Make sure you have already installed `dnsmasq`
# Please modify the variables according to your need
# Don't forget to change the name of network interface
# Check them with `ifconfig`

ip_address="192.168.5.1"
netmask="255.255.255.0"
dhcp_range_start="192.168.5.50"
dhcp_range_end="192.168.5.100"
dhcp_time="12h"
dns_server="1.1.1.1"
eth="eth0"
wlan="wlan0"

sudo iptables -F
sudo iptables -t nat -F
sudo iptables -t nat -A POSTROUTING -o $wlan -j MASQUERADE
sudo iptables -A FORWARD -i $wlan -o $eth -m state --state RELATED,ESTABLISHED -j ACCEPT
sudo iptables -A FORWARD -i $eth -o $wlan -j ACCEPT

sudo sh -c "echo 1 > /proc/sys/net/ipv4/ip_forward"

sudo ifconfig $eth down
sudo ifconfig $eth up
sudo ifconfig $eth $ip_address netmask $netmask

# make sure the service is not running
sudo systemctl stop dnsmasq

# kill any running instance, otherwise will fail to bind the IP
sudo pkill dnsmasq
sudo dnsmasq -A /#/$ip_address

sudo pkill python3
sh -c "cd ~/src/WaybackProxy; sudo python3 waybackproxy.py" &

~/.dotnet/dotnet ~/TimeMachine/InternetTimeMachine.dll