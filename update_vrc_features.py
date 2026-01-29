import requests
import urllib.parse
import time

PARAM_PATH = "Assets/✮exto/✮3.0/centi param.asset"
MENU_PATH = "Assets/✮exto/✮3.0/centi main menu.asset"

def add_param(name, ptype):
    encoded_path = urllib.parse.quote(PARAM_PATH)
    url = f"http://localhost:8085/vrc/param/add?path={encoded_path}&name={name}&type={ptype}&saved=true"
    requests.get(url)

def rebuild_menu():
    encoded_path = urllib.parse.quote(MENU_PATH)
    requests.get(f"http://localhost:8085/vrc/menu/clear?path={encoded_path}")
    time.sleep(1)
    
    def add_control(name, ctype, param="", sub=""):
        enc_name = urllib.parse.quote(name)
        enc_sub = urllib.parse.quote(sub) if sub else ""
        url = f"http://localhost:8085/vrc/menu/add?path={encoded_path}&name={enc_name}&type={ctype}&parameter={param}&subMenu={enc_sub}"
        requests.get(url)

    add_control("Clothes and Accessories", 2, sub="Assets/✮exto/✮3.0/clothes and acessories.asset")
    add_control("animal parts", 2, sub="Assets/✮exto/✮3.0/animal parts.asset")
    add_control("gogo", 2, sub="Assets/✮exto/Kali Locomotion/GoMenus/GoAllMenu.asset")
    add_control("fun", 2, sub="Assets/✮exto/✮3.0/fun.asset")
    add_control("Marker", 2, sub="Assets/VRLabs/GeneratedAssets/Marker/centipede boyo fix/Marker Menu.asset")
    add_control("Horn Toggle", 1, param="Horns")
    add_control("Color Wheel", 4, param="Color")
    add_control("Color Pitch", 4, param="ColorPitch")

print("Adding ColorPitch parameter...")
add_param("ColorPitch", 1) # Float
print("Rebuilding menu with Color Pitch...")
rebuild_menu()
print("Done.")
