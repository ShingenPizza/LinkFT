import json
import datetime
from json import JSONEncoder


def main():
    with open('module.json', 'r') as f:
        data = json.load(f)
    versionstr = data['Version']
    x, y, z = versionstr.split('.')
    z, s = z.split('-')
    data['Version'] = f'{x}.{y}.{z}-{int(s) + 1}'
    data['LastUpdated'] = datetime.datetime.now().isoformat()
    with open('module.json', 'w', newline='\n') as f:
        json.dump(data, f, ensure_ascii=False, indent=4)


if __name__ == '__main__':
    main()
