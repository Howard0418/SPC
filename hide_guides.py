import os
import glob
import re

directory = r'd:\SPC\frontend\mes-spc-web\src\views'
files = glob.glob(os.path.join(directory, '*.vue'))

count = 0
for file in files:
    with open(file, 'r', encoding='utf-8') as f:
        content = f.read()
    
    # Add v-if="false" to the div right after the guide comment
    new_content = re.sub(r'(<!-- Guide[^\n]*-->\s*<div )(?!(v-if="false" ))', r'\1v-if="false" ', content)
    
    if new_content != content:
        with open(file, 'w', encoding='utf-8') as f:
            f.write(new_content)
        count += 1
        print(f'Updated {file}')

print(f'Total files updated: {count}')
