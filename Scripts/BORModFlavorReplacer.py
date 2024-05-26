import xml.dom.minidom as xml
import pandas as pd
import os
import re

path = "..\\Common\\"
out_path = "data.csv"
debug_msg = True

def debug(message):
    if debug_msg:
        print(message)

def getChildByChildrenTagName(node, name):
    nlist = []
    for child in node.childNodes:
        for child2 in child.childNodes:
            if child2.nodeType == xml.Node.ELEMENT_NODE and child2.tagName == name:
                nlist.append(child)
    return nlist
            

def all_files_in_dir(dirPath = path):
    if dirPath == path:
        debug("scanning directory xml\\ for files...")
    flist = []
    for dir_item in os.scandir(dirPath):
        if dir_item.is_file() and dir_item.path[-4:] == ".xml":
            debug("file found at path: " + dir_item.path)
            flist.append(dir_item.path)
        elif dir_item.is_dir():
            flist.extend(all_files_in_dir(dir_item.path))
    if dirPath == path:
        debug(f'Found {len(flist)} files to process')
    return flist

def process_files():
    data = {"type": [], "name": [], "label": [], "desc": [], "file_name": []}
    for file in all_files_in_dir():
        process_file(data, file)
    pd.DataFrame(data).to_csv(out_path)
    debug(f'data output to {out_path}')

def process_file(data, file):
    debug(f'processing file {file}')
    dom = xml.parse(file).firstChild
    elements = getChildByChildrenTagName(dom, "description")
    debug(f'found {len(elements)} nodes to process')
    for element in elements:
        if not element.hasAttribute("Abstract"):
            data["type"].append(element.tagName)
            data["name"].append(element.getElementsByTagName("defName")[0].childNodes[0].data)
            data["label"].append(element.getElementsByTagName("label")[0].childNodes[0].data)
            data["desc"].append(element.getElementsByTagName("description")[0].childNodes[0].data)
            data["file_name"].append(file)

def log(msg):
    print(f'{workTicker:03d}: {msg}')

def getChildByTagName(node, name):
    for child in node.childNodes:
        if child.nodeType == xml.Node.ELEMENT_NODE and child.tagName == name:
            return child

def findChild(node: xml.Node, type:str, name:str) -> xml.Node: #Explicitly tooled for this program
    for child in node.childNodes:
        if child.nodeType == xml.Node.ELEMENT_NODE and not child.hasAttribute("Abstract") and child.tagName == type: #TODO: test against data within child text node, not tagName...
            for child2 in child.childNodes:
                if child2.nodeType == xml.Node.ELEMENT_NODE and child2.tagName == 'defName' and child2.childNodes[0].data == name:
                    return child
    return None

workTicker = 0
labChanges = 0
descChanges = 0

def replaceLabelAndDesc(path, type, name, label, desc):
    log(f'Appraising {type} named \"{name}\" in file: \"{path}\"...')
    file_content = ''
    with open(path, 'r', encoding='utf-8', errors='replace') as file:
        file_content = file.read()
    file_content = re.sub('\s+(?=<)', '', file_content)
    file_content = file_content.replace('\ufffd', '')
    doc = xml.parseString(file_content)
    dom = doc.firstChild
    node = findChild(dom, type, name)
    if node is None:
        log(f'No {type} named \"{name}\" in {path}')
        return
    log(f'Found {type} \"{name}\"')
    labNode = getChildByTagName(node, 'label')
    changeLab = labNode.childNodes[0].data != label.replace('\ufffd', '\'')
    descNode = getChildByTagName(node, 'description')
    changeDesc = descNode.childNodes[0].data != desc.replace('\ufffd', '\'')

    if not changeLab and not changeDesc:
        log(f'No changes necessary.')
    else:
        labStr = '' if changeLab else ' not'
        descStr = '' if changeDesc else ' not'
        log(f'Label will{labStr} be changed; description will{descStr} be changed')
    
    if changeLab:
        labNode.childNodes[0].data = label
        global labChanges
        labChanges += 1
    if changeDesc:
        descNode.childNodes[0].data = desc
        global descChanges
        descChanges += 1
    if changeLab or changeDesc:
        with open(path, 'w') as file:
            doc.writexml(file, '', '\t', '\n', 'utf-8')
        log(f'Changes saved to {path}')
    doc.unlink()


def read_replacements(data_path = "data.csv"):
    global workTicker
    data = pd.read_csv(data_path)
    for res in data.iterrows():
        workTicker += 1
        row = res[1]
        replaceLabelAndDesc(row['file_name'], row['type'], row['name'], row['label'], row['desc'])
    log(f'{labChanges + descChanges} changes made: {labChanges} labels & {descChanges} descriptions changed.')
    

if __name__ == "__main__":
    read_replacements()

