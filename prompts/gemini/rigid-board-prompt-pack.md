# Gemini Rigid Board Prompt Pack

Use this pack for future sprite-board generation and for any rat regenerations. These prompts are self-contained and do not depend on earlier images.
They also do not assume any prior knowledge of the project name.

## Order

1. `rat / male / red`
2. `rat / male / orange`
3. `rat / male / yellow`
4. `rat / male / blue`
5. `rat / male / indigo`
6. `rat / male / violet`
7. `rat / female / red`
8. `rat / female / orange`
9. `rat / female / yellow`
10. `rat / female / blue`
11. `rat / female / indigo`
12. `rat / female / violet`
13. `crow / male / red`
14. `crow / male / orange`
15. `crow / male / yellow`
16. `crow / male / blue`
17. `crow / male / indigo`
18. `crow / male / violet`
19. `crow / female / red`
20. `crow / female / orange`
21. `crow / female / yellow`
22. `crow / female / blue`
23. `crow / female / indigo`
24. `crow / female / violet`
25. `fox / male / red`
26. `fox / male / orange`
27. `fox / male / yellow`
28. `fox / male / blue`
29. `fox / male / indigo`
30. `fox / male / violet`
31. `fox / female / red`
32. `fox / female / orange`
33. `fox / female / yellow`
34. `fox / female / blue`
35. `fox / female / indigo`
36. `fox / female / violet`
37. `snake / male / red`
38. `snake / male / orange`
39. `snake / male / yellow`
40. `snake / male / blue`
41. `snake / male / indigo`
42. `snake / male / violet`
43. `snake / female / red`
44. `snake / female / orange`
45. `snake / female / yellow`
46. `snake / female / blue`
47. `snake / female / indigo`
48. `snake / female / violet`
49. `deer / male / red`
50. `deer / male / orange`
51. `deer / male / yellow`
52. `deer / male / blue`
53. `deer / male / indigo`
54. `deer / male / violet`
55. `deer / female / red`
56. `deer / female / orange`
57. `deer / female / yellow`
58. `deer / female / blue`
59. `deer / female / indigo`
60. `deer / female / violet`
61. `frog / male / red`
62. `frog / male / orange`
63. `frog / male / yellow`
64. `frog / male / blue`
65. `frog / male / indigo`
66. `frog / male / violet`
67. `frog / female / red`
68. `frog / female / orange`
69. `frog / female / yellow`
70. `frog / female / blue`
71. `frog / female / indigo`
72. `frog / female / violet`
73. `pigeon / male / red`
74. `pigeon / male / orange`
75. `pigeon / male / yellow`
76. `pigeon / male / blue`
77. `pigeon / male / indigo`
78. `pigeon / male / violet`
79. `pigeon / female / red`
80. `pigeon / female / orange`
81. `pigeon / female / yellow`
82. `pigeon / female / blue`
83. `pigeon / female / indigo`
84. `pigeon / female / violet`
85. `raccoon / male / red`
86. `raccoon / male / orange`
87. `raccoon / male / yellow`
88. `raccoon / male / blue`
89. `raccoon / male / indigo`
90. `raccoon / male / violet`
91. `raccoon / female / red`
92. `raccoon / female / orange`
93. `raccoon / female / yellow`
94. `raccoon / female / blue`
95. `raccoon / female / indigo`
96. `raccoon / female / violet`
97. `squirrel / male / red`
98. `squirrel / male / orange`
99. `squirrel / male / yellow`
100. `squirrel / male / blue`
101. `squirrel / male / indigo`
102. `squirrel / male / violet`
103. `squirrel / female / red`
104. `squirrel / female / orange`
105. `squirrel / female / yellow`
106. `squirrel / female / blue`
107. `squirrel / female / indigo`
108. `squirrel / female / violet`
109. `goose / male / red`
110. `goose / male / orange`
111. `goose / male / yellow`
112. `goose / male / blue`
113. `goose / male / indigo`
114. `goose / male / violet`
115. `goose / female / red`
116. `goose / female / orange`
117. `goose / female / yellow`
118. `goose / female / blue`
119. `goose / female / indigo`
120. `goose / female / violet`

## 1. rat / male / red

```text
Generate an IMAGE ONLY.

Do not answer with text.
Do not describe limitations.
Do not provide configuration data.
Do not provide JSON.
Do not provide a table.
Do not provide a written explanation.

Create exactly one labeled sprite reference board image for this single variant only, for a small 2D desktop virtual pet game:

rat / male / red

This prompt is self-contained. Do not rely on previous images.

Board rules:
- one single board only
- exactly 5 columns and exactly 6 rows
- exactly 30 total cells
- every row must contain exactly 5 cells
- every column must contain exactly 6 cells
- all cells must be equal size
- no merged cells
- no missing cells
- no extra cells
- no irregular row widths
- one sprite per cell
- one frame label at the bottom of each cell
- visible border lines between cells
- no title banner
- no extra decorations
- no extra animals
- no extra variants

Exact frame order:
Row 1: idle_00, idle_01, idle_02, idle_03, walk_00
Row 2: walk_01, walk_02, walk_03, walk_04, walk_05
Row 3: eat_00, eat_01, eat_02, eat_03, happy_00
Row 4: happy_01, happy_02, happy_03, sad_00, sad_01
Row 5: sleep_00, sleep_01, sick_00, sick_01, sick_02
Row 6: sick_03, bathe_00, bathe_01, bathe_02, bathe_03

Critical rules:
- exactly these 30 frames only
- do not skip frames
- do not duplicate frames
- do not invent extra walk frames
- do not rename any frame
- labels must exactly match the frame names above
- keep all cells evenly sized
- keep the sprite centered above the label in each cell
- keep the board as a rigid contact-sheet grid
- do not turn the board into a poster or model sheet

Style:
- pixel art
- crisp edges
- slightly moody, cute, vulnerable desktop-pet tone
- compact rat body, rounded ears, whisker-forward face, thin tail, scrappy but cute
- male should read slightly larger and bolder
- red should be the dominant palette family without losing rat readability

Animation intent:
- idle: subtle breathing or blinking
- walk: readable side-view walk cycle
- eat: nibble or drink motion
- happy: excited positive motion
- sad: slumped low-energy pose
- sleep: clearly asleep and distinct from idle
- sick: visibly unwell and distinct from sad
- bathe: self-cleaning or washing motion

Return only the generated image.
```

## 2. rat / male / orange

```text
Generate an IMAGE ONLY.

Do not answer with text.
Do not describe limitations.
Do not provide configuration data.
Do not provide JSON.
Do not provide a table.
Do not provide a written explanation.

Create exactly one labeled sprite reference board image for this single variant only:

rat / male / orange

This prompt is self-contained. Do not rely on previous images.

Board rules:
- one single board only
- exactly 5 columns and exactly 6 rows
- exactly 30 total cells
- every row must contain exactly 5 cells
- every column must contain exactly 6 cells
- all cells must be equal size
- no merged cells
- no missing cells
- no extra cells
- no irregular row widths
- one sprite per cell
- one frame label at the bottom of each cell
- visible border lines between cells
- no title banner
- no extra decorations
- no extra animals
- no extra variants

Exact frame order:
Row 1: idle_00, idle_01, idle_02, idle_03, walk_00
Row 2: walk_01, walk_02, walk_03, walk_04, walk_05
Row 3: eat_00, eat_01, eat_02, eat_03, happy_00
Row 4: happy_01, happy_02, happy_03, sad_00, sad_01
Row 5: sleep_00, sleep_01, sick_00, sick_01, sick_02
Row 6: sick_03, bathe_00, bathe_01, bathe_02, bathe_03

Critical rules:
- exactly these 30 frames only
- do not skip frames
- do not duplicate frames
- do not invent extra walk frames
- do not rename any frame
- labels must exactly match the frame names above
- keep all cells evenly sized
- keep the sprite centered above the label in each cell
- keep the board as a rigid contact-sheet grid
- do not turn the board into a poster or model sheet

Style:
- pixel art
- crisp edges
- slightly moody, cute, vulnerable desktop-pet tone
- compact rat body, rounded ears, whisker-forward face, thin tail, scrappy but cute
- male should read slightly larger and bolder
- orange should be the dominant palette family without losing rat readability

Animation intent:
- idle: subtle breathing or blinking
- walk: readable side-view walk cycle
- eat: nibble or drink motion
- happy: excited positive motion
- sad: slumped low-energy pose
- sleep: clearly asleep and distinct from idle
- sick: visibly unwell and distinct from sad
- bathe: self-cleaning or washing motion

Return only the generated image.
```

## 3. rat / male / yellow

```text
Generate an IMAGE ONLY.

Do not answer with text.
Do not describe limitations.
Do not provide configuration data.
Do not provide JSON.
Do not provide a table.
Do not provide a written explanation.

Create exactly one labeled sprite reference board image for this single variant only:

rat / male / yellow

This prompt is self-contained. Do not rely on previous images.

Board rules:
- one single board only
- exactly 5 columns and exactly 6 rows
- exactly 30 total cells
- every row must contain exactly 5 cells
- every column must contain exactly 6 cells
- all cells must be equal size
- no merged cells
- no missing cells
- no extra cells
- no irregular row widths
- one sprite per cell
- one frame label at the bottom of each cell
- visible border lines between cells
- no title banner
- no extra decorations
- no extra animals
- no extra variants

Exact frame order:
Row 1: idle_00, idle_01, idle_02, idle_03, walk_00
Row 2: walk_01, walk_02, walk_03, walk_04, walk_05
Row 3: eat_00, eat_01, eat_02, eat_03, happy_00
Row 4: happy_01, happy_02, happy_03, sad_00, sad_01
Row 5: sleep_00, sleep_01, sick_00, sick_01, sick_02
Row 6: sick_03, bathe_00, bathe_01, bathe_02, bathe_03

Critical rules:
- exactly these 30 frames only
- do not skip frames
- do not duplicate frames
- do not invent extra walk frames
- do not rename any frame
- labels must exactly match the frame names above
- keep all cells evenly sized
- keep the sprite centered above the label in each cell
- keep the board as a rigid contact-sheet grid
- do not turn the board into a poster or model sheet

Style:
- pixel art
- crisp edges
- slightly moody, cute, vulnerable desktop-pet tone
- compact rat body, rounded ears, whisker-forward face, thin tail, scrappy but cute
- male should read slightly larger and bolder
- yellow should be the dominant palette family without losing rat readability

Animation intent:
- idle: subtle breathing or blinking
- walk: readable side-view walk cycle
- eat: nibble or drink motion
- happy: excited positive motion
- sad: slumped low-energy pose
- sleep: clearly asleep and distinct from idle
- sick: visibly unwell and distinct from sad
- bathe: self-cleaning or washing motion

Return only the generated image.
```

## 4. rat / male / blue

```text
Generate an IMAGE ONLY.

Do not answer with text.
Do not describe limitations.
Do not provide configuration data.
Do not provide JSON.
Do not provide a table.
Do not provide a written explanation.

Create exactly one labeled sprite reference board image for this single variant only:

rat / male / blue

This prompt is self-contained. Do not rely on previous images.

Board rules:
- one single board only
- exactly 5 columns and exactly 6 rows
- exactly 30 total cells
- every row must contain exactly 5 cells
- every column must contain exactly 6 cells
- all cells must be equal size
- no merged cells
- no missing cells
- no extra cells
- no irregular row widths
- one sprite per cell
- one frame label at the bottom of each cell
- visible border lines between cells
- no title banner
- no extra decorations
- no extra animals
- no extra variants

Exact frame order:
Row 1: idle_00, idle_01, idle_02, idle_03, walk_00
Row 2: walk_01, walk_02, walk_03, walk_04, walk_05
Row 3: eat_00, eat_01, eat_02, eat_03, happy_00
Row 4: happy_01, happy_02, happy_03, sad_00, sad_01
Row 5: sleep_00, sleep_01, sick_00, sick_01, sick_02
Row 6: sick_03, bathe_00, bathe_01, bathe_02, bathe_03

Critical rules:
- exactly these 30 frames only
- do not skip frames
- do not duplicate frames
- do not invent extra walk frames
- do not rename any frame
- labels must exactly match the frame names above
- keep all cells evenly sized
- keep the sprite centered above the label in each cell
- keep the board as a rigid contact-sheet grid
- do not turn the board into a poster or model sheet

Style:
- pixel art
- crisp edges
- slightly moody, cute, vulnerable desktop-pet tone
- compact rat body, rounded ears, whisker-forward face, thin tail, scrappy but cute
- male should read slightly larger and bolder
- blue should be the dominant palette family without losing rat readability

Animation intent:
- idle: subtle breathing or blinking
- walk: readable side-view walk cycle
- eat: nibble or drink motion
- happy: excited positive motion
- sad: slumped low-energy pose
- sleep: clearly asleep and distinct from idle
- sick: visibly unwell and distinct from sad
- bathe: self-cleaning or washing motion

Return only the generated image.
```

## 5. rat / male / indigo

```text
Generate an IMAGE ONLY.

Do not answer with text.
Do not describe limitations.
Do not provide configuration data.
Do not provide JSON.
Do not provide a table.
Do not provide a written explanation.

Create exactly one labeled sprite reference board image for this single variant only:

rat / male / indigo

This prompt is self-contained. Do not rely on previous images.

Board rules:
- one single board only
- exactly 5 columns and exactly 6 rows
- exactly 30 total cells
- every row must contain exactly 5 cells
- every column must contain exactly 6 cells
- all cells must be equal size
- no merged cells
- no missing cells
- no extra cells
- no irregular row widths
- one sprite per cell
- one frame label at the bottom of each cell
- visible border lines between cells
- no title banner
- no extra decorations
- no extra animals
- no extra variants

Exact frame order:
Row 1: idle_00, idle_01, idle_02, idle_03, walk_00
Row 2: walk_01, walk_02, walk_03, walk_04, walk_05
Row 3: eat_00, eat_01, eat_02, eat_03, happy_00
Row 4: happy_01, happy_02, happy_03, sad_00, sad_01
Row 5: sleep_00, sleep_01, sick_00, sick_01, sick_02
Row 6: sick_03, bathe_00, bathe_01, bathe_02, bathe_03

Critical rules:
- exactly these 30 frames only
- do not skip frames
- do not duplicate frames
- do not invent extra walk frames
- do not rename any frame
- labels must exactly match the frame names above
- keep all cells evenly sized
- keep the sprite centered above the label in each cell
- keep the board as a rigid contact-sheet grid
- do not turn the board into a poster or model sheet

Style:
- pixel art
- crisp edges
- slightly moody, cute, vulnerable desktop-pet tone
- compact rat body, rounded ears, whisker-forward face, thin tail, scrappy but cute
- male should read slightly larger and bolder
- indigo should be the dominant palette family without losing rat readability

Animation intent:
- idle: subtle breathing or blinking
- walk: readable side-view walk cycle
- eat: nibble or drink motion
- happy: excited positive motion
- sad: slumped low-energy pose
- sleep: clearly asleep and distinct from idle
- sick: visibly unwell and distinct from sad
- bathe: self-cleaning or washing motion

Return only the generated image.
```

## 6. rat / male / violet

```text
Generate an IMAGE ONLY.

Do not answer with text.
Do not describe limitations.
Do not provide configuration data.
Do not provide JSON.
Do not provide a table.
Do not provide a written explanation.

Create exactly one labeled sprite reference board image for this single variant only:

rat / male / violet

This prompt is self-contained. Do not rely on previous images.

Board rules:
- one single board only
- exactly 5 columns and exactly 6 rows
- exactly 30 total cells
- every row must contain exactly 5 cells
- every column must contain exactly 6 cells
- all cells must be equal size
- no merged cells
- no missing cells
- no extra cells
- no irregular row widths
- one sprite per cell
- one frame label at the bottom of each cell
- visible border lines between cells
- no title banner
- no extra decorations
- no extra animals
- no extra variants

Exact frame order:
Row 1: idle_00, idle_01, idle_02, idle_03, walk_00
Row 2: walk_01, walk_02, walk_03, walk_04, walk_05
Row 3: eat_00, eat_01, eat_02, eat_03, happy_00
Row 4: happy_01, happy_02, happy_03, sad_00, sad_01
Row 5: sleep_00, sleep_01, sick_00, sick_01, sick_02
Row 6: sick_03, bathe_00, bathe_01, bathe_02, bathe_03

Critical rules:
- exactly these 30 frames only
- do not skip frames
- do not duplicate frames
- do not invent extra walk frames
- do not rename any frame
- labels must exactly match the frame names above
- keep all cells evenly sized
- keep the sprite centered above the label in each cell
- keep the board as a rigid contact-sheet grid
- do not turn the board into a poster or model sheet

Style:
- pixel art
- crisp edges
- slightly moody, cute, vulnerable desktop-pet tone
- compact rat body, rounded ears, whisker-forward face, thin tail, scrappy but cute
- male should read slightly larger and bolder
- violet should be the dominant palette family without losing rat readability

Animation intent:
- idle: subtle breathing or blinking
- walk: readable side-view walk cycle
- eat: nibble or drink motion
- happy: excited positive motion
- sad: slumped low-energy pose
- sleep: clearly asleep and distinct from idle
- sick: visibly unwell and distinct from sad
- bathe: self-cleaning or washing motion

Return only the generated image.
```

## 7. rat / female / red

```text
Generate an IMAGE ONLY.

Do not answer with text.
Do not describe limitations.
Do not provide configuration data.
Do not provide JSON.
Do not provide a table.
Do not provide a written explanation.

Create exactly one labeled sprite reference board image for this single variant only:

rat / female / red

This prompt is self-contained. Do not rely on previous images.

Board rules:
- one single board only
- exactly 5 columns and exactly 6 rows
- exactly 30 total cells
- every row must contain exactly 5 cells
- every column must contain exactly 6 cells
- all cells must be equal size
- no merged cells
- no missing cells
- no extra cells
- no irregular row widths
- one sprite per cell
- one frame label at the bottom of each cell
- visible border lines between cells
- no title banner
- no extra decorations
- no extra animals
- no extra variants

Exact frame order:
Row 1: idle_00, idle_01, idle_02, idle_03, walk_00
Row 2: walk_01, walk_02, walk_03, walk_04, walk_05
Row 3: eat_00, eat_01, eat_02, eat_03, happy_00
Row 4: happy_01, happy_02, happy_03, sad_00, sad_01
Row 5: sleep_00, sleep_01, sick_00, sick_01, sick_02
Row 6: sick_03, bathe_00, bathe_01, bathe_02, bathe_03

Critical rules:
- exactly these 30 frames only
- do not skip frames
- do not duplicate frames
- do not invent extra walk frames
- do not rename any frame
- labels must exactly match the frame names above
- keep all cells evenly sized
- keep the sprite centered above the label in each cell
- keep the board as a rigid contact-sheet grid
- do not turn the board into a poster or model sheet

Style:
- pixel art
- crisp edges
- slightly moody, cute, vulnerable desktop-pet tone
- compact rat body, rounded ears, whisker-forward face, thin tail, scrappy but cute
- female should read slightly smaller and calmer
- red should be the dominant palette family without losing rat readability

Animation intent:
- idle: subtle breathing or blinking
- walk: readable side-view walk cycle
- eat: nibble or drink motion
- happy: excited positive motion
- sad: slumped low-energy pose
- sleep: clearly asleep and distinct from idle
- sick: visibly unwell and distinct from sad
- bathe: self-cleaning or washing motion

Return only the generated image.
```

## 8. rat / female / orange

```text
Generate an IMAGE ONLY.

Do not answer with text.
Do not describe limitations.
Do not provide configuration data.
Do not provide JSON.
Do not provide a table.
Do not provide a written explanation.

Create exactly one labeled sprite reference board image for this single variant only:

rat / female / orange

This prompt is self-contained. Do not rely on previous images.

Board rules:
- one single board only
- exactly 5 columns and exactly 6 rows
- exactly 30 total cells
- every row must contain exactly 5 cells
- every column must contain exactly 6 cells
- all cells must be equal size
- no merged cells
- no missing cells
- no extra cells
- no irregular row widths
- one sprite per cell
- one frame label at the bottom of each cell
- visible border lines between cells
- no title banner
- no extra decorations
- no extra animals
- no extra variants

Exact frame order:
Row 1: idle_00, idle_01, idle_02, idle_03, walk_00
Row 2: walk_01, walk_02, walk_03, walk_04, walk_05
Row 3: eat_00, eat_01, eat_02, eat_03, happy_00
Row 4: happy_01, happy_02, happy_03, sad_00, sad_01
Row 5: sleep_00, sleep_01, sick_00, sick_01, sick_02
Row 6: sick_03, bathe_00, bathe_01, bathe_02, bathe_03

Critical rules:
- exactly these 30 frames only
- do not skip frames
- do not duplicate frames
- do not invent extra walk frames
- do not rename any frame
- labels must exactly match the frame names above
- keep all cells evenly sized
- keep the sprite centered above the label in each cell
- keep the board as a rigid contact-sheet grid
- do not turn the board into a poster or model sheet

Style:
- pixel art
- crisp edges
- slightly moody, cute, vulnerable desktop-pet tone
- compact rat body, rounded ears, whisker-forward face, thin tail, scrappy but cute
- female should read slightly smaller and calmer
- orange should be the dominant palette family without losing rat readability

Animation intent:
- idle: subtle breathing or blinking
- walk: readable side-view walk cycle
- eat: nibble or drink motion
- happy: excited positive motion
- sad: slumped low-energy pose
- sleep: clearly asleep and distinct from idle
- sick: visibly unwell and distinct from sad
- bathe: self-cleaning or washing motion

Return only the generated image.
```

## 9. rat / female / yellow

```text
Generate an IMAGE ONLY.

Do not answer with text.
Do not describe limitations.
Do not provide configuration data.
Do not provide JSON.
Do not provide a table.
Do not provide a written explanation.

Create exactly one labeled sprite reference board image for this single variant only:

rat / female / yellow

This prompt is self-contained. Do not rely on previous images.

Board rules:
- one single board only
- exactly 5 columns and exactly 6 rows
- exactly 30 total cells
- every row must contain exactly 5 cells
- every column must contain exactly 6 cells
- all cells must be equal size
- no merged cells
- no missing cells
- no extra cells
- no irregular row widths
- one sprite per cell
- one frame label at the bottom of each cell
- visible border lines between cells
- no title banner
- no extra decorations
- no extra animals
- no extra variants

Exact frame order:
Row 1: idle_00, idle_01, idle_02, idle_03, walk_00
Row 2: walk_01, walk_02, walk_03, walk_04, walk_05
Row 3: eat_00, eat_01, eat_02, eat_03, happy_00
Row 4: happy_01, happy_02, happy_03, sad_00, sad_01
Row 5: sleep_00, sleep_01, sick_00, sick_01, sick_02
Row 6: sick_03, bathe_00, bathe_01, bathe_02, bathe_03

Critical rules:
- exactly these 30 frames only
- do not skip frames
- do not duplicate frames
- do not invent extra walk frames
- do not rename any frame
- labels must exactly match the frame names above
- keep all cells evenly sized
- keep the sprite centered above the label in each cell
- keep the board as a rigid contact-sheet grid
- do not turn the board into a poster or model sheet

Style:
- pixel art
- crisp edges
- slightly moody, cute, vulnerable desktop-pet tone
- compact rat body, rounded ears, whisker-forward face, thin tail, scrappy but cute
- female should read slightly smaller and calmer
- yellow should be the dominant palette family without losing rat readability

Animation intent:
- idle: subtle breathing or blinking
- walk: readable side-view walk cycle
- eat: nibble or drink motion
- happy: excited positive motion
- sad: slumped low-energy pose
- sleep: clearly asleep and distinct from idle
- sick: visibly unwell and distinct from sad
- bathe: self-cleaning or washing motion

Return only the generated image.
```

## 10. rat / female / blue

```text
Generate an IMAGE ONLY.

Do not answer with text.
Do not describe limitations.
Do not provide configuration data.
Do not provide JSON.
Do not provide a table.
Do not provide a written explanation.

Create exactly one labeled sprite reference board image for this single variant only:

rat / female / blue

This prompt is self-contained. Do not rely on previous images.

Board rules:
- one single board only
- exactly 5 columns and exactly 6 rows
- exactly 30 total cells
- every row must contain exactly 5 cells
- every column must contain exactly 6 cells
- all cells must be equal size
- no merged cells
- no missing cells
- no extra cells
- no irregular row widths
- one sprite per cell
- one frame label at the bottom of each cell
- visible border lines between cells
- no title banner
- no extra decorations
- no extra animals
- no extra variants

Exact frame order:
Row 1: idle_00, idle_01, idle_02, idle_03, walk_00
Row 2: walk_01, walk_02, walk_03, walk_04, walk_05
Row 3: eat_00, eat_01, eat_02, eat_03, happy_00
Row 4: happy_01, happy_02, happy_03, sad_00, sad_01
Row 5: sleep_00, sleep_01, sick_00, sick_01, sick_02
Row 6: sick_03, bathe_00, bathe_01, bathe_02, bathe_03

Critical rules:
- exactly these 30 frames only
- do not skip frames
- do not duplicate frames
- do not invent extra walk frames
- do not rename any frame
- labels must exactly match the frame names above
- keep all cells evenly sized
- keep the sprite centered above the label in each cell
- keep the board as a rigid contact-sheet grid
- do not turn the board into a poster or model sheet

Style:
- pixel art
- crisp edges
- slightly moody, cute, vulnerable desktop-pet tone
- compact rat body, rounded ears, whisker-forward face, thin tail, scrappy but cute
- female should read slightly smaller and calmer
- blue should be the dominant palette family without losing rat readability

Animation intent:
- idle: subtle breathing or blinking
- walk: readable side-view walk cycle
- eat: nibble or drink motion
- happy: excited positive motion
- sad: slumped low-energy pose
- sleep: clearly asleep and distinct from idle
- sick: visibly unwell and distinct from sad
- bathe: self-cleaning or washing motion

Return only the generated image.
```

## 11. rat / female / indigo

```text
Generate an IMAGE ONLY.

Do not answer with text.
Do not describe limitations.
Do not provide configuration data.
Do not provide JSON.
Do not provide a table.
Do not provide a written explanation.

Create exactly one labeled sprite reference board image for this single variant only:

rat / female / indigo

This prompt is self-contained. Do not rely on previous images.

Board rules:
- one single board only
- exactly 5 columns and exactly 6 rows
- exactly 30 total cells
- every row must contain exactly 5 cells
- every column must contain exactly 6 cells
- all cells must be equal size
- no merged cells
- no missing cells
- no extra cells
- no irregular row widths
- one sprite per cell
- one frame label at the bottom of each cell
- visible border lines between cells
- no title banner
- no extra decorations
- no extra animals
- no extra variants

Exact frame order:
Row 1: idle_00, idle_01, idle_02, idle_03, walk_00
Row 2: walk_01, walk_02, walk_03, walk_04, walk_05
Row 3: eat_00, eat_01, eat_02, eat_03, happy_00
Row 4: happy_01, happy_02, happy_03, sad_00, sad_01
Row 5: sleep_00, sleep_01, sick_00, sick_01, sick_02
Row 6: sick_03, bathe_00, bathe_01, bathe_02, bathe_03

Critical rules:
- exactly these 30 frames only
- do not skip frames
- do not duplicate frames
- do not invent extra walk frames
- do not rename any frame
- labels must exactly match the frame names above
- keep all cells evenly sized
- keep the sprite centered above the label in each cell
- keep the board as a rigid contact-sheet grid
- do not turn the board into a poster or model sheet

Style:
- pixel art
- crisp edges
- slightly moody, cute, vulnerable desktop-pet tone
- compact rat body, rounded ears, whisker-forward face, thin tail, scrappy but cute
- female should read slightly smaller and calmer
- indigo should be the dominant palette family without losing rat readability

Animation intent:
- idle: subtle breathing or blinking
- walk: readable side-view walk cycle
- eat: nibble or drink motion
- happy: excited positive motion
- sad: slumped low-energy pose
- sleep: clearly asleep and distinct from idle
- sick: visibly unwell and distinct from sad
- bathe: self-cleaning or washing motion

Return only the generated image.
```

## 12. rat / female / violet

```text
Generate an IMAGE ONLY.

Do not answer with text.
Do not describe limitations.
Do not provide configuration data.
Do not provide JSON.
Do not provide a table.
Do not provide a written explanation.

Create exactly one labeled sprite reference board image for this single variant only:

rat / female / violet

This prompt is self-contained. Do not rely on previous images.

Board rules:
- one single board only
- exactly 5 columns and exactly 6 rows
- exactly 30 total cells
- every row must contain exactly 5 cells
- every column must contain exactly 6 cells
- all cells must be equal size
- no merged cells
- no missing cells
- no extra cells
- no irregular row widths
- one sprite per cell
- one frame label at the bottom of each cell
- visible border lines between cells
- no title banner
- no extra decorations
- no extra animals
- no extra variants

Exact frame order:
Row 1: idle_00, idle_01, idle_02, idle_03, walk_00
Row 2: walk_01, walk_02, walk_03, walk_04, walk_05
Row 3: eat_00, eat_01, eat_02, eat_03, happy_00
Row 4: happy_01, happy_02, happy_03, sad_00, sad_01
Row 5: sleep_00, sleep_01, sick_00, sick_01, sick_02
Row 6: sick_03, bathe_00, bathe_01, bathe_02, bathe_03

Critical rules:
- exactly these 30 frames only
- do not skip frames
- do not duplicate frames
- do not invent extra walk frames
- do not rename any frame
- labels must exactly match the frame names above
- keep all cells evenly sized
- keep the sprite centered above the label in each cell
- keep the board as a rigid contact-sheet grid
- do not turn the board into a poster or model sheet

Style:
- pixel art
- crisp edges
- slightly moody, cute, vulnerable desktop-pet tone
- compact rat body, rounded ears, whisker-forward face, thin tail, scrappy but cute
- female should read slightly smaller and calmer
- violet should be the dominant palette family without losing rat readability

Animation intent:
- idle: subtle breathing or blinking
- walk: readable side-view walk cycle
- eat: nibble or drink motion
- happy: excited positive motion
- sad: slumped low-energy pose
- sleep: clearly asleep and distinct from idle
- sick: visibly unwell and distinct from sad
- bathe: self-cleaning or washing motion

Return only the generated image.
```

## 13. crow / male / red

```text
Generate an IMAGE ONLY.

Do not answer with text.
Do not describe limitations.
Do not provide configuration data.
Do not provide JSON.
Do not provide a table.
Do not provide a written explanation.

Create exactly one labeled sprite reference board image for this single variant only:

crow / male / red

This prompt is self-contained. Do not rely on previous images.

Board rules:
- one single board only
- exactly 5 columns and exactly 6 rows
- exactly 30 total cells
- every row must contain exactly 5 cells
- every column must contain exactly 6 cells
- all cells must be equal size
- no merged cells
- no missing cells
- no extra cells
- no irregular row widths
- one sprite per cell
- one frame label at the bottom of each cell
- visible border lines between cells
- no title banner
- no extra decorations
- no extra animals
- no extra variants

Exact frame order:
Row 1: idle_00, idle_01, idle_02, idle_03, walk_00
Row 2: walk_01, walk_02, walk_03, walk_04, walk_05
Row 3: eat_00, eat_01, eat_02, eat_03, happy_00
Row 4: happy_01, happy_02, happy_03, sad_00, sad_01
Row 5: sleep_00, sleep_01, sick_00, sick_01, sick_02
Row 6: sick_03, bathe_00, bathe_01, bathe_02, bathe_03

Critical rules:
- exactly these 30 frames only
- do not skip frames
- do not duplicate frames
- do not invent extra walk frames
- do not rename any frame
- labels must exactly match the frame names above
- keep all cells evenly sized
- keep the sprite centered above the label in each cell
- keep the board as a rigid contact-sheet grid
- do not turn the board into a poster or model sheet

Style:
- pixel art
- crisp edges
- slightly moody, cute, vulnerable desktop-pet tone
- sharp beak, angular posture, sleek silhouette, smarter and more pointed than pigeon
- male should read slightly larger and bolder
- red should be the dominant palette family without losing crow readability

Animation intent:
- idle: subtle breathing or blinking
- walk: readable side-view walk cycle
- eat: nibble or drink motion
- happy: excited positive motion
- sad: slumped low-energy pose
- sleep: clearly asleep and distinct from idle
- sick: visibly unwell and distinct from sad
- bathe: self-cleaning or washing motion

Return only the generated image.
```

## 14. crow / male / orange

```text
Generate an IMAGE ONLY.

Do not answer with text.
Do not describe limitations.
Do not provide configuration data.
Do not provide JSON.
Do not provide a table.
Do not provide a written explanation.

Create exactly one labeled sprite reference board image for this single variant only:

crow / male / orange

This prompt is self-contained. Do not rely on previous images.

Board rules:
- one single board only
- exactly 5 columns and exactly 6 rows
- exactly 30 total cells
- every row must contain exactly 5 cells
- every column must contain exactly 6 cells
- all cells must be equal size
- no merged cells
- no missing cells
- no extra cells
- no irregular row widths
- one sprite per cell
- one frame label at the bottom of each cell
- visible border lines between cells
- no title banner
- no extra decorations
- no extra animals
- no extra variants

Exact frame order:
Row 1: idle_00, idle_01, idle_02, idle_03, walk_00
Row 2: walk_01, walk_02, walk_03, walk_04, walk_05
Row 3: eat_00, eat_01, eat_02, eat_03, happy_00
Row 4: happy_01, happy_02, happy_03, sad_00, sad_01
Row 5: sleep_00, sleep_01, sick_00, sick_01, sick_02
Row 6: sick_03, bathe_00, bathe_01, bathe_02, bathe_03

Critical rules:
- exactly these 30 frames only
- do not skip frames
- do not duplicate frames
- do not invent extra walk frames
- do not rename any frame
- labels must exactly match the frame names above
- keep all cells evenly sized
- keep the sprite centered above the label in each cell
- keep the board as a rigid contact-sheet grid
- do not turn the board into a poster or model sheet

Style:
- pixel art
- crisp edges
- slightly moody, cute, vulnerable desktop-pet tone
- sharp beak, angular posture, sleek silhouette, smarter and more pointed than pigeon
- male should read slightly larger and bolder
- orange should be the dominant palette family without losing crow readability

Animation intent:
- idle: subtle breathing or blinking
- walk: readable side-view walk cycle
- eat: nibble or drink motion
- happy: excited positive motion
- sad: slumped low-energy pose
- sleep: clearly asleep and distinct from idle
- sick: visibly unwell and distinct from sad
- bathe: self-cleaning or washing motion

Return only the generated image.
```

## 15. crow / male / yellow

```text
Generate an IMAGE ONLY.

Do not answer with text.
Do not describe limitations.
Do not provide configuration data.
Do not provide JSON.
Do not provide a table.
Do not provide a written explanation.

Create exactly one labeled sprite reference board image for this single variant only:

crow / male / yellow

This prompt is self-contained. Do not rely on previous images.

Board rules:
- one single board only
- exactly 5 columns and exactly 6 rows
- exactly 30 total cells
- every row must contain exactly 5 cells
- every column must contain exactly 6 cells
- all cells must be equal size
- no merged cells
- no missing cells
- no extra cells
- no irregular row widths
- one sprite per cell
- one frame label at the bottom of each cell
- visible border lines between cells
- no title banner
- no extra decorations
- no extra animals
- no extra variants

Exact frame order:
Row 1: idle_00, idle_01, idle_02, idle_03, walk_00
Row 2: walk_01, walk_02, walk_03, walk_04, walk_05
Row 3: eat_00, eat_01, eat_02, eat_03, happy_00
Row 4: happy_01, happy_02, happy_03, sad_00, sad_01
Row 5: sleep_00, sleep_01, sick_00, sick_01, sick_02
Row 6: sick_03, bathe_00, bathe_01, bathe_02, bathe_03

Critical rules:
- exactly these 30 frames only
- do not skip frames
- do not duplicate frames
- do not invent extra walk frames
- do not rename any frame
- labels must exactly match the frame names above
- keep all cells evenly sized
- keep the sprite centered above the label in each cell
- keep the board as a rigid contact-sheet grid
- do not turn the board into a poster or model sheet

Style:
- pixel art
- crisp edges
- slightly moody, cute, vulnerable desktop-pet tone
- sharp beak, angular posture, sleek silhouette, smarter and more pointed than pigeon
- male should read slightly larger and bolder
- yellow should be the dominant palette family without losing crow readability

Animation intent:
- idle: subtle breathing or blinking
- walk: readable side-view walk cycle
- eat: nibble or drink motion
- happy: excited positive motion
- sad: slumped low-energy pose
- sleep: clearly asleep and distinct from idle
- sick: visibly unwell and distinct from sad
- bathe: self-cleaning or washing motion

Return only the generated image.
```

## 16. crow / male / blue

```text
Generate an IMAGE ONLY.

Do not answer with text.
Do not describe limitations.
Do not provide configuration data.
Do not provide JSON.
Do not provide a table.
Do not provide a written explanation.

Create exactly one labeled sprite reference board image for this single variant only:

crow / male / blue

This prompt is self-contained. Do not rely on previous images.

Board rules:
- one single board only
- exactly 5 columns and exactly 6 rows
- exactly 30 total cells
- every row must contain exactly 5 cells
- every column must contain exactly 6 cells
- all cells must be equal size
- no merged cells
- no missing cells
- no extra cells
- no irregular row widths
- one sprite per cell
- one frame label at the bottom of each cell
- visible border lines between cells
- no title banner
- no extra decorations
- no extra animals
- no extra variants

Exact frame order:
Row 1: idle_00, idle_01, idle_02, idle_03, walk_00
Row 2: walk_01, walk_02, walk_03, walk_04, walk_05
Row 3: eat_00, eat_01, eat_02, eat_03, happy_00
Row 4: happy_01, happy_02, happy_03, sad_00, sad_01
Row 5: sleep_00, sleep_01, sick_00, sick_01, sick_02
Row 6: sick_03, bathe_00, bathe_01, bathe_02, bathe_03

Critical rules:
- exactly these 30 frames only
- do not skip frames
- do not duplicate frames
- do not invent extra walk frames
- do not rename any frame
- labels must exactly match the frame names above
- keep all cells evenly sized
- keep the sprite centered above the label in each cell
- keep the board as a rigid contact-sheet grid
- do not turn the board into a poster or model sheet

Style:
- pixel art
- crisp edges
- slightly moody, cute, vulnerable desktop-pet tone
- sharp beak, angular posture, sleek silhouette, smarter and more pointed than pigeon
- male should read slightly larger and bolder
- blue should be the dominant palette family without losing crow readability

Animation intent:
- idle: subtle breathing or blinking
- walk: readable side-view walk cycle
- eat: nibble or drink motion
- happy: excited positive motion
- sad: slumped low-energy pose
- sleep: clearly asleep and distinct from idle
- sick: visibly unwell and distinct from sad
- bathe: self-cleaning or washing motion

Return only the generated image.
```

## 17. crow / male / indigo

```text
Generate an IMAGE ONLY.

Do not answer with text.
Do not describe limitations.
Do not provide configuration data.
Do not provide JSON.
Do not provide a table.
Do not provide a written explanation.

Create exactly one labeled sprite reference board image for this single variant only:

crow / male / indigo

This prompt is self-contained. Do not rely on previous images.

Board rules:
- one single board only
- exactly 5 columns and exactly 6 rows
- exactly 30 total cells
- every row must contain exactly 5 cells
- every column must contain exactly 6 cells
- all cells must be equal size
- no merged cells
- no missing cells
- no extra cells
- no irregular row widths
- one sprite per cell
- one frame label at the bottom of each cell
- visible border lines between cells
- no title banner
- no extra decorations
- no extra animals
- no extra variants

Exact frame order:
Row 1: idle_00, idle_01, idle_02, idle_03, walk_00
Row 2: walk_01, walk_02, walk_03, walk_04, walk_05
Row 3: eat_00, eat_01, eat_02, eat_03, happy_00
Row 4: happy_01, happy_02, happy_03, sad_00, sad_01
Row 5: sleep_00, sleep_01, sick_00, sick_01, sick_02
Row 6: sick_03, bathe_00, bathe_01, bathe_02, bathe_03

Critical rules:
- exactly these 30 frames only
- do not skip frames
- do not duplicate frames
- do not invent extra walk frames
- do not rename any frame
- labels must exactly match the frame names above
- keep all cells evenly sized
- keep the sprite centered above the label in each cell
- keep the board as a rigid contact-sheet grid
- do not turn the board into a poster or model sheet

Style:
- pixel art
- crisp edges
- slightly moody, cute, vulnerable desktop-pet tone
- sharp beak, angular posture, sleek silhouette, smarter and more pointed than pigeon
- male should read slightly larger and bolder
- indigo should be the dominant palette family without losing crow readability

Animation intent:
- idle: subtle breathing or blinking
- walk: readable side-view walk cycle
- eat: nibble or drink motion
- happy: excited positive motion
- sad: slumped low-energy pose
- sleep: clearly asleep and distinct from idle
- sick: visibly unwell and distinct from sad
- bathe: self-cleaning or washing motion

Return only the generated image.
```

## 18. crow / male / violet

```text
Generate an IMAGE ONLY.

Do not answer with text.
Do not describe limitations.
Do not provide configuration data.
Do not provide JSON.
Do not provide a table.
Do not provide a written explanation.

Create exactly one labeled sprite reference board image for this single variant only:

crow / male / violet

This prompt is self-contained. Do not rely on previous images.

Board rules:
- one single board only
- exactly 5 columns and exactly 6 rows
- exactly 30 total cells
- every row must contain exactly 5 cells
- every column must contain exactly 6 cells
- all cells must be equal size
- no merged cells
- no missing cells
- no extra cells
- no irregular row widths
- one sprite per cell
- one frame label at the bottom of each cell
- visible border lines between cells
- no title banner
- no extra decorations
- no extra animals
- no extra variants

Exact frame order:
Row 1: idle_00, idle_01, idle_02, idle_03, walk_00
Row 2: walk_01, walk_02, walk_03, walk_04, walk_05
Row 3: eat_00, eat_01, eat_02, eat_03, happy_00
Row 4: happy_01, happy_02, happy_03, sad_00, sad_01
Row 5: sleep_00, sleep_01, sick_00, sick_01, sick_02
Row 6: sick_03, bathe_00, bathe_01, bathe_02, bathe_03

Critical rules:
- exactly these 30 frames only
- do not skip frames
- do not duplicate frames
- do not invent extra walk frames
- do not rename any frame
- labels must exactly match the frame names above
- keep all cells evenly sized
- keep the sprite centered above the label in each cell
- keep the board as a rigid contact-sheet grid
- do not turn the board into a poster or model sheet

Style:
- pixel art
- crisp edges
- slightly moody, cute, vulnerable desktop-pet tone
- sharp beak, angular posture, sleek silhouette, smarter and more pointed than pigeon
- male should read slightly larger and bolder
- violet should be the dominant palette family without losing crow readability

Animation intent:
- idle: subtle breathing or blinking
- walk: readable side-view walk cycle
- eat: nibble or drink motion
- happy: excited positive motion
- sad: slumped low-energy pose
- sleep: clearly asleep and distinct from idle
- sick: visibly unwell and distinct from sad
- bathe: self-cleaning or washing motion

Return only the generated image.
```

## 19. crow / female / red

```text
Generate an IMAGE ONLY.

Do not answer with text.
Do not describe limitations.
Do not provide configuration data.
Do not provide JSON.
Do not provide a table.
Do not provide a written explanation.

Create exactly one labeled sprite reference board image for this single variant only:

crow / female / red

This prompt is self-contained. Do not rely on previous images.

Board rules:
- one single board only
- exactly 5 columns and exactly 6 rows
- exactly 30 total cells
- every row must contain exactly 5 cells
- every column must contain exactly 6 cells
- all cells must be equal size
- no merged cells
- no missing cells
- no extra cells
- no irregular row widths
- one sprite per cell
- one frame label at the bottom of each cell
- visible border lines between cells
- no title banner
- no extra decorations
- no extra animals
- no extra variants

Exact frame order:
Row 1: idle_00, idle_01, idle_02, idle_03, walk_00
Row 2: walk_01, walk_02, walk_03, walk_04, walk_05
Row 3: eat_00, eat_01, eat_02, eat_03, happy_00
Row 4: happy_01, happy_02, happy_03, sad_00, sad_01
Row 5: sleep_00, sleep_01, sick_00, sick_01, sick_02
Row 6: sick_03, bathe_00, bathe_01, bathe_02, bathe_03

Critical rules:
- exactly these 30 frames only
- do not skip frames
- do not duplicate frames
- do not invent extra walk frames
- do not rename any frame
- labels must exactly match the frame names above
- keep all cells evenly sized
- keep the sprite centered above the label in each cell
- keep the board as a rigid contact-sheet grid
- do not turn the board into a poster or model sheet

Style:
- pixel art
- crisp edges
- slightly moody, cute, vulnerable desktop-pet tone
- sharp beak, angular posture, sleek silhouette, smarter and more pointed than pigeon
- female should read slightly smaller and calmer
- red should be the dominant palette family without losing crow readability

Animation intent:
- idle: subtle breathing or blinking
- walk: readable side-view walk cycle
- eat: nibble or drink motion
- happy: excited positive motion
- sad: slumped low-energy pose
- sleep: clearly asleep and distinct from idle
- sick: visibly unwell and distinct from sad
- bathe: self-cleaning or washing motion

Return only the generated image.
```

## 20. crow / female / orange

```text
Generate an IMAGE ONLY.

Do not answer with text.
Do not describe limitations.
Do not provide configuration data.
Do not provide JSON.
Do not provide a table.
Do not provide a written explanation.

Create exactly one labeled sprite reference board image for this single variant only:

crow / female / orange

This prompt is self-contained. Do not rely on previous images.

Board rules:
- one single board only
- exactly 5 columns and exactly 6 rows
- exactly 30 total cells
- every row must contain exactly 5 cells
- every column must contain exactly 6 cells
- all cells must be equal size
- no merged cells
- no missing cells
- no extra cells
- no irregular row widths
- one sprite per cell
- one frame label at the bottom of each cell
- visible border lines between cells
- no title banner
- no extra decorations
- no extra animals
- no extra variants

Exact frame order:
Row 1: idle_00, idle_01, idle_02, idle_03, walk_00
Row 2: walk_01, walk_02, walk_03, walk_04, walk_05
Row 3: eat_00, eat_01, eat_02, eat_03, happy_00
Row 4: happy_01, happy_02, happy_03, sad_00, sad_01
Row 5: sleep_00, sleep_01, sick_00, sick_01, sick_02
Row 6: sick_03, bathe_00, bathe_01, bathe_02, bathe_03

Critical rules:
- exactly these 30 frames only
- do not skip frames
- do not duplicate frames
- do not invent extra walk frames
- do not rename any frame
- labels must exactly match the frame names above
- keep all cells evenly sized
- keep the sprite centered above the label in each cell
- keep the board as a rigid contact-sheet grid
- do not turn the board into a poster or model sheet

Style:
- pixel art
- crisp edges
- slightly moody, cute, vulnerable desktop-pet tone
- sharp beak, angular posture, sleek silhouette, smarter and more pointed than pigeon
- female should read slightly smaller and calmer
- orange should be the dominant palette family without losing crow readability

Animation intent:
- idle: subtle breathing or blinking
- walk: readable side-view walk cycle
- eat: nibble or drink motion
- happy: excited positive motion
- sad: slumped low-energy pose
- sleep: clearly asleep and distinct from idle
- sick: visibly unwell and distinct from sad
- bathe: self-cleaning or washing motion

Return only the generated image.
```

## 21. crow / female / yellow

```text
Generate an IMAGE ONLY.

Do not answer with text.
Do not describe limitations.
Do not provide configuration data.
Do not provide JSON.
Do not provide a table.
Do not provide a written explanation.

Create exactly one labeled sprite reference board image for this single variant only:

crow / female / yellow

This prompt is self-contained. Do not rely on previous images.

Board rules:
- one single board only
- exactly 5 columns and exactly 6 rows
- exactly 30 total cells
- every row must contain exactly 5 cells
- every column must contain exactly 6 cells
- all cells must be equal size
- no merged cells
- no missing cells
- no extra cells
- no irregular row widths
- one sprite per cell
- one frame label at the bottom of each cell
- visible border lines between cells
- no title banner
- no extra decorations
- no extra animals
- no extra variants

Exact frame order:
Row 1: idle_00, idle_01, idle_02, idle_03, walk_00
Row 2: walk_01, walk_02, walk_03, walk_04, walk_05
Row 3: eat_00, eat_01, eat_02, eat_03, happy_00
Row 4: happy_01, happy_02, happy_03, sad_00, sad_01
Row 5: sleep_00, sleep_01, sick_00, sick_01, sick_02
Row 6: sick_03, bathe_00, bathe_01, bathe_02, bathe_03

Critical rules:
- exactly these 30 frames only
- do not skip frames
- do not duplicate frames
- do not invent extra walk frames
- do not rename any frame
- labels must exactly match the frame names above
- keep all cells evenly sized
- keep the sprite centered above the label in each cell
- keep the board as a rigid contact-sheet grid
- do not turn the board into a poster or model sheet

Style:
- pixel art
- crisp edges
- slightly moody, cute, vulnerable desktop-pet tone
- sharp beak, angular posture, sleek silhouette, smarter and more pointed than pigeon
- female should read slightly smaller and calmer
- yellow should be the dominant palette family without losing crow readability

Animation intent:
- idle: subtle breathing or blinking
- walk: readable side-view walk cycle
- eat: nibble or drink motion
- happy: excited positive motion
- sad: slumped low-energy pose
- sleep: clearly asleep and distinct from idle
- sick: visibly unwell and distinct from sad
- bathe: self-cleaning or washing motion

Return only the generated image.
```

## 22. crow / female / blue

```text
Generate an IMAGE ONLY.

Do not answer with text.
Do not describe limitations.
Do not provide configuration data.
Do not provide JSON.
Do not provide a table.
Do not provide a written explanation.

Create exactly one labeled sprite reference board image for this single variant only:

crow / female / blue

This prompt is self-contained. Do not rely on previous images.

Board rules:
- one single board only
- exactly 5 columns and exactly 6 rows
- exactly 30 total cells
- every row must contain exactly 5 cells
- every column must contain exactly 6 cells
- all cells must be equal size
- no merged cells
- no missing cells
- no extra cells
- no irregular row widths
- one sprite per cell
- one frame label at the bottom of each cell
- visible border lines between cells
- no title banner
- no extra decorations
- no extra animals
- no extra variants

Exact frame order:
Row 1: idle_00, idle_01, idle_02, idle_03, walk_00
Row 2: walk_01, walk_02, walk_03, walk_04, walk_05
Row 3: eat_00, eat_01, eat_02, eat_03, happy_00
Row 4: happy_01, happy_02, happy_03, sad_00, sad_01
Row 5: sleep_00, sleep_01, sick_00, sick_01, sick_02
Row 6: sick_03, bathe_00, bathe_01, bathe_02, bathe_03

Critical rules:
- exactly these 30 frames only
- do not skip frames
- do not duplicate frames
- do not invent extra walk frames
- do not rename any frame
- labels must exactly match the frame names above
- keep all cells evenly sized
- keep the sprite centered above the label in each cell
- keep the board as a rigid contact-sheet grid
- do not turn the board into a poster or model sheet

Style:
- pixel art
- crisp edges
- slightly moody, cute, vulnerable desktop-pet tone
- sharp beak, angular posture, sleek silhouette, smarter and more pointed than pigeon
- female should read slightly smaller and calmer
- blue should be the dominant palette family without losing crow readability

Animation intent:
- idle: subtle breathing or blinking
- walk: readable side-view walk cycle
- eat: nibble or drink motion
- happy: excited positive motion
- sad: slumped low-energy pose
- sleep: clearly asleep and distinct from idle
- sick: visibly unwell and distinct from sad
- bathe: self-cleaning or washing motion

Return only the generated image.
```

## 23. crow / female / indigo

```text
Generate an IMAGE ONLY.

Do not answer with text.
Do not describe limitations.
Do not provide configuration data.
Do not provide JSON.
Do not provide a table.
Do not provide a written explanation.

Create exactly one labeled sprite reference board image for this single variant only:

crow / female / indigo

This prompt is self-contained. Do not rely on previous images.

Board rules:
- one single board only
- exactly 5 columns and exactly 6 rows
- exactly 30 total cells
- every row must contain exactly 5 cells
- every column must contain exactly 6 cells
- all cells must be equal size
- no merged cells
- no missing cells
- no extra cells
- no irregular row widths
- one sprite per cell
- one frame label at the bottom of each cell
- visible border lines between cells
- no title banner
- no extra decorations
- no extra animals
- no extra variants

Exact frame order:
Row 1: idle_00, idle_01, idle_02, idle_03, walk_00
Row 2: walk_01, walk_02, walk_03, walk_04, walk_05
Row 3: eat_00, eat_01, eat_02, eat_03, happy_00
Row 4: happy_01, happy_02, happy_03, sad_00, sad_01
Row 5: sleep_00, sleep_01, sick_00, sick_01, sick_02
Row 6: sick_03, bathe_00, bathe_01, bathe_02, bathe_03

Critical rules:
- exactly these 30 frames only
- do not skip frames
- do not duplicate frames
- do not invent extra walk frames
- do not rename any frame
- labels must exactly match the frame names above
- keep all cells evenly sized
- keep the sprite centered above the label in each cell
- keep the board as a rigid contact-sheet grid
- do not turn the board into a poster or model sheet

Style:
- pixel art
- crisp edges
- slightly moody, cute, vulnerable desktop-pet tone
- sharp beak, angular posture, sleek silhouette, smarter and more pointed than pigeon
- female should read slightly smaller and calmer
- indigo should be the dominant palette family without losing crow readability

Animation intent:
- idle: subtle breathing or blinking
- walk: readable side-view walk cycle
- eat: nibble or drink motion
- happy: excited positive motion
- sad: slumped low-energy pose
- sleep: clearly asleep and distinct from idle
- sick: visibly unwell and distinct from sad
- bathe: self-cleaning or washing motion

Return only the generated image.
```

## 24. crow / female / violet

```text
Generate an IMAGE ONLY.

Do not answer with text.
Do not describe limitations.
Do not provide configuration data.
Do not provide JSON.
Do not provide a table.
Do not provide a written explanation.

Create exactly one labeled sprite reference board image for this single variant only:

crow / female / violet

This prompt is self-contained. Do not rely on previous images.

Board rules:
- one single board only
- exactly 5 columns and exactly 6 rows
- exactly 30 total cells
- every row must contain exactly 5 cells
- every column must contain exactly 6 cells
- all cells must be equal size
- no merged cells
- no missing cells
- no extra cells
- no irregular row widths
- one sprite per cell
- one frame label at the bottom of each cell
- visible border lines between cells
- no title banner
- no extra decorations
- no extra animals
- no extra variants

Exact frame order:
Row 1: idle_00, idle_01, idle_02, idle_03, walk_00
Row 2: walk_01, walk_02, walk_03, walk_04, walk_05
Row 3: eat_00, eat_01, eat_02, eat_03, happy_00
Row 4: happy_01, happy_02, happy_03, sad_00, sad_01
Row 5: sleep_00, sleep_01, sick_00, sick_01, sick_02
Row 6: sick_03, bathe_00, bathe_01, bathe_02, bathe_03

Critical rules:
- exactly these 30 frames only
- do not skip frames
- do not duplicate frames
- do not invent extra walk frames
- do not rename any frame
- labels must exactly match the frame names above
- keep all cells evenly sized
- keep the sprite centered above the label in each cell
- keep the board as a rigid contact-sheet grid
- do not turn the board into a poster or model sheet

Style:
- pixel art
- crisp edges
- slightly moody, cute, vulnerable desktop-pet tone
- sharp beak, angular posture, sleek silhouette, smarter and more pointed than pigeon
- female should read slightly smaller and calmer
- violet should be the dominant palette family without losing crow readability

Animation intent:
- idle: subtle breathing or blinking
- walk: readable side-view walk cycle
- eat: nibble or drink motion
- happy: excited positive motion
- sad: slumped low-energy pose
- sleep: clearly asleep and distinct from idle
- sick: visibly unwell and distinct from sad
- bathe: self-cleaning or washing motion

Return only the generated image.
```

## 25. fox / male / red

```text
Generate an IMAGE ONLY.

Do not answer with text.
Do not describe limitations.
Do not provide configuration data.
Do not provide JSON.
Do not provide a table.
Do not provide a written explanation.

Create exactly one labeled sprite reference board image for this single variant only:

fox / male / red

This prompt is self-contained. Do not rely on previous images.

Board rules:
- one single board only
- exactly 5 columns and exactly 6 rows
- exactly 30 total cells
- every row must contain exactly 5 cells
- every column must contain exactly 6 cells
- all cells must be equal size
- no merged cells
- no missing cells
- no extra cells
- no irregular row widths
- one sprite per cell
- one frame label at the bottom of each cell
- visible border lines between cells
- no title banner
- no extra decorations
- no extra animals
- no extra variants

Exact frame order:
Row 1: idle_00, idle_01, idle_02, idle_03, walk_00
Row 2: walk_01, walk_02, walk_03, walk_04, walk_05
Row 3: eat_00, eat_01, eat_02, eat_03, happy_00
Row 4: happy_01, happy_02, happy_03, sad_00, sad_01
Row 5: sleep_00, sleep_01, sick_00, sick_01, sick_02
Row 6: sick_03, bathe_00, bathe_01, bathe_02, bathe_03

Critical rules:
- exactly these 30 frames only
- do not skip frames
- do not duplicate frames
- do not invent extra walk frames
- do not rename any frame
- labels must exactly match the frame names above
- keep all cells evenly sized
- keep the sprite centered above the label in each cell
- keep the board as a rigid contact-sheet grid
- do not turn the board into a poster or model sheet

Style:
- pixel art
- crisp edges
- slightly moody, cute, vulnerable desktop-pet tone
- fluffy tail essential, pointed ears, agile silhouette, clever face
- male should read slightly larger and bolder
- red should be the dominant palette family without losing fox readability

Animation intent:
- idle: subtle breathing or blinking
- walk: readable side-view walk cycle
- eat: nibble or drink motion
- happy: excited positive motion
- sad: slumped low-energy pose
- sleep: clearly asleep and distinct from idle
- sick: visibly unwell and distinct from sad
- bathe: self-cleaning or washing motion

Return only the generated image.
```

## 26. fox / male / orange

```text
Generate an IMAGE ONLY.

Do not answer with text.
Do not describe limitations.
Do not provide configuration data.
Do not provide JSON.
Do not provide a table.
Do not provide a written explanation.

Create exactly one labeled sprite reference board image for this single variant only:

fox / male / orange

This prompt is self-contained. Do not rely on previous images.

Board rules:
- one single board only
- exactly 5 columns and exactly 6 rows
- exactly 30 total cells
- every row must contain exactly 5 cells
- every column must contain exactly 6 cells
- all cells must be equal size
- no merged cells
- no missing cells
- no extra cells
- no irregular row widths
- one sprite per cell
- one frame label at the bottom of each cell
- visible border lines between cells
- no title banner
- no extra decorations
- no extra animals
- no extra variants

Exact frame order:
Row 1: idle_00, idle_01, idle_02, idle_03, walk_00
Row 2: walk_01, walk_02, walk_03, walk_04, walk_05
Row 3: eat_00, eat_01, eat_02, eat_03, happy_00
Row 4: happy_01, happy_02, happy_03, sad_00, sad_01
Row 5: sleep_00, sleep_01, sick_00, sick_01, sick_02
Row 6: sick_03, bathe_00, bathe_01, bathe_02, bathe_03

Critical rules:
- exactly these 30 frames only
- do not skip frames
- do not duplicate frames
- do not invent extra walk frames
- do not rename any frame
- labels must exactly match the frame names above
- keep all cells evenly sized
- keep the sprite centered above the label in each cell
- keep the board as a rigid contact-sheet grid
- do not turn the board into a poster or model sheet

Style:
- pixel art
- crisp edges
- slightly moody, cute, vulnerable desktop-pet tone
- fluffy tail essential, pointed ears, agile silhouette, clever face
- male should read slightly larger and bolder
- orange should be the dominant palette family without losing fox readability

Animation intent:
- idle: subtle breathing or blinking
- walk: readable side-view walk cycle
- eat: nibble or drink motion
- happy: excited positive motion
- sad: slumped low-energy pose
- sleep: clearly asleep and distinct from idle
- sick: visibly unwell and distinct from sad
- bathe: self-cleaning or washing motion

Return only the generated image.
```

## 27. fox / male / yellow

```text
Generate an IMAGE ONLY.

Do not answer with text.
Do not describe limitations.
Do not provide configuration data.
Do not provide JSON.
Do not provide a table.
Do not provide a written explanation.

Create exactly one labeled sprite reference board image for this single variant only:

fox / male / yellow

This prompt is self-contained. Do not rely on previous images.

Board rules:
- one single board only
- exactly 5 columns and exactly 6 rows
- exactly 30 total cells
- every row must contain exactly 5 cells
- every column must contain exactly 6 cells
- all cells must be equal size
- no merged cells
- no missing cells
- no extra cells
- no irregular row widths
- one sprite per cell
- one frame label at the bottom of each cell
- visible border lines between cells
- no title banner
- no extra decorations
- no extra animals
- no extra variants

Exact frame order:
Row 1: idle_00, idle_01, idle_02, idle_03, walk_00
Row 2: walk_01, walk_02, walk_03, walk_04, walk_05
Row 3: eat_00, eat_01, eat_02, eat_03, happy_00
Row 4: happy_01, happy_02, happy_03, sad_00, sad_01
Row 5: sleep_00, sleep_01, sick_00, sick_01, sick_02
Row 6: sick_03, bathe_00, bathe_01, bathe_02, bathe_03

Critical rules:
- exactly these 30 frames only
- do not skip frames
- do not duplicate frames
- do not invent extra walk frames
- do not rename any frame
- labels must exactly match the frame names above
- keep all cells evenly sized
- keep the sprite centered above the label in each cell
- keep the board as a rigid contact-sheet grid
- do not turn the board into a poster or model sheet

Style:
- pixel art
- crisp edges
- slightly moody, cute, vulnerable desktop-pet tone
- fluffy tail essential, pointed ears, agile silhouette, clever face
- male should read slightly larger and bolder
- yellow should be the dominant palette family without losing fox readability

Animation intent:
- idle: subtle breathing or blinking
- walk: readable side-view walk cycle
- eat: nibble or drink motion
- happy: excited positive motion
- sad: slumped low-energy pose
- sleep: clearly asleep and distinct from idle
- sick: visibly unwell and distinct from sad
- bathe: self-cleaning or washing motion

Return only the generated image.
```

## 28. fox / male / blue

```text
Generate an IMAGE ONLY.

Do not answer with text.
Do not describe limitations.
Do not provide configuration data.
Do not provide JSON.
Do not provide a table.
Do not provide a written explanation.

Create exactly one labeled sprite reference board image for this single variant only:

fox / male / blue

This prompt is self-contained. Do not rely on previous images.

Board rules:
- one single board only
- exactly 5 columns and exactly 6 rows
- exactly 30 total cells
- every row must contain exactly 5 cells
- every column must contain exactly 6 cells
- all cells must be equal size
- no merged cells
- no missing cells
- no extra cells
- no irregular row widths
- one sprite per cell
- one frame label at the bottom of each cell
- visible border lines between cells
- no title banner
- no extra decorations
- no extra animals
- no extra variants

Exact frame order:
Row 1: idle_00, idle_01, idle_02, idle_03, walk_00
Row 2: walk_01, walk_02, walk_03, walk_04, walk_05
Row 3: eat_00, eat_01, eat_02, eat_03, happy_00
Row 4: happy_01, happy_02, happy_03, sad_00, sad_01
Row 5: sleep_00, sleep_01, sick_00, sick_01, sick_02
Row 6: sick_03, bathe_00, bathe_01, bathe_02, bathe_03

Critical rules:
- exactly these 30 frames only
- do not skip frames
- do not duplicate frames
- do not invent extra walk frames
- do not rename any frame
- labels must exactly match the frame names above
- keep all cells evenly sized
- keep the sprite centered above the label in each cell
- keep the board as a rigid contact-sheet grid
- do not turn the board into a poster or model sheet

Style:
- pixel art
- crisp edges
- slightly moody, cute, vulnerable desktop-pet tone
- fluffy tail essential, pointed ears, agile silhouette, clever face
- male should read slightly larger and bolder
- blue should be the dominant palette family without losing fox readability

Animation intent:
- idle: subtle breathing or blinking
- walk: readable side-view walk cycle
- eat: nibble or drink motion
- happy: excited positive motion
- sad: slumped low-energy pose
- sleep: clearly asleep and distinct from idle
- sick: visibly unwell and distinct from sad
- bathe: self-cleaning or washing motion

Return only the generated image.
```

## 29. fox / male / indigo

```text
Generate an IMAGE ONLY.

Do not answer with text.
Do not describe limitations.
Do not provide configuration data.
Do not provide JSON.
Do not provide a table.
Do not provide a written explanation.

Create exactly one labeled sprite reference board image for this single variant only:

fox / male / indigo

This prompt is self-contained. Do not rely on previous images.

Board rules:
- one single board only
- exactly 5 columns and exactly 6 rows
- exactly 30 total cells
- every row must contain exactly 5 cells
- every column must contain exactly 6 cells
- all cells must be equal size
- no merged cells
- no missing cells
- no extra cells
- no irregular row widths
- one sprite per cell
- one frame label at the bottom of each cell
- visible border lines between cells
- no title banner
- no extra decorations
- no extra animals
- no extra variants

Exact frame order:
Row 1: idle_00, idle_01, idle_02, idle_03, walk_00
Row 2: walk_01, walk_02, walk_03, walk_04, walk_05
Row 3: eat_00, eat_01, eat_02, eat_03, happy_00
Row 4: happy_01, happy_02, happy_03, sad_00, sad_01
Row 5: sleep_00, sleep_01, sick_00, sick_01, sick_02
Row 6: sick_03, bathe_00, bathe_01, bathe_02, bathe_03

Critical rules:
- exactly these 30 frames only
- do not skip frames
- do not duplicate frames
- do not invent extra walk frames
- do not rename any frame
- labels must exactly match the frame names above
- keep all cells evenly sized
- keep the sprite centered above the label in each cell
- keep the board as a rigid contact-sheet grid
- do not turn the board into a poster or model sheet

Style:
- pixel art
- crisp edges
- slightly moody, cute, vulnerable desktop-pet tone
- fluffy tail essential, pointed ears, agile silhouette, clever face
- male should read slightly larger and bolder
- indigo should be the dominant palette family without losing fox readability

Animation intent:
- idle: subtle breathing or blinking
- walk: readable side-view walk cycle
- eat: nibble or drink motion
- happy: excited positive motion
- sad: slumped low-energy pose
- sleep: clearly asleep and distinct from idle
- sick: visibly unwell and distinct from sad
- bathe: self-cleaning or washing motion

Return only the generated image.
```

## 30. fox / male / violet

```text
Generate an IMAGE ONLY.

Do not answer with text.
Do not describe limitations.
Do not provide configuration data.
Do not provide JSON.
Do not provide a table.
Do not provide a written explanation.

Create exactly one labeled sprite reference board image for this single variant only:

fox / male / violet

This prompt is self-contained. Do not rely on previous images.

Board rules:
- one single board only
- exactly 5 columns and exactly 6 rows
- exactly 30 total cells
- every row must contain exactly 5 cells
- every column must contain exactly 6 cells
- all cells must be equal size
- no merged cells
- no missing cells
- no extra cells
- no irregular row widths
- one sprite per cell
- one frame label at the bottom of each cell
- visible border lines between cells
- no title banner
- no extra decorations
- no extra animals
- no extra variants

Exact frame order:
Row 1: idle_00, idle_01, idle_02, idle_03, walk_00
Row 2: walk_01, walk_02, walk_03, walk_04, walk_05
Row 3: eat_00, eat_01, eat_02, eat_03, happy_00
Row 4: happy_01, happy_02, happy_03, sad_00, sad_01
Row 5: sleep_00, sleep_01, sick_00, sick_01, sick_02
Row 6: sick_03, bathe_00, bathe_01, bathe_02, bathe_03

Critical rules:
- exactly these 30 frames only
- do not skip frames
- do not duplicate frames
- do not invent extra walk frames
- do not rename any frame
- labels must exactly match the frame names above
- keep all cells evenly sized
- keep the sprite centered above the label in each cell
- keep the board as a rigid contact-sheet grid
- do not turn the board into a poster or model sheet

Style:
- pixel art
- crisp edges
- slightly moody, cute, vulnerable desktop-pet tone
- fluffy tail essential, pointed ears, agile silhouette, clever face
- male should read slightly larger and bolder
- violet should be the dominant palette family without losing fox readability

Animation intent:
- idle: subtle breathing or blinking
- walk: readable side-view walk cycle
- eat: nibble or drink motion
- happy: excited positive motion
- sad: slumped low-energy pose
- sleep: clearly asleep and distinct from idle
- sick: visibly unwell and distinct from sad
- bathe: self-cleaning or washing motion

Return only the generated image.
```

## 31. fox / female / red

```text
Generate an IMAGE ONLY.

Do not answer with text.
Do not describe limitations.
Do not provide configuration data.
Do not provide JSON.
Do not provide a table.
Do not provide a written explanation.

Create exactly one labeled sprite reference board image for this single variant only:

fox / female / red

This prompt is self-contained. Do not rely on previous images.

Board rules:
- one single board only
- exactly 5 columns and exactly 6 rows
- exactly 30 total cells
- every row must contain exactly 5 cells
- every column must contain exactly 6 cells
- all cells must be equal size
- no merged cells
- no missing cells
- no extra cells
- no irregular row widths
- one sprite per cell
- one frame label at the bottom of each cell
- visible border lines between cells
- no title banner
- no extra decorations
- no extra animals
- no extra variants

Exact frame order:
Row 1: idle_00, idle_01, idle_02, idle_03, walk_00
Row 2: walk_01, walk_02, walk_03, walk_04, walk_05
Row 3: eat_00, eat_01, eat_02, eat_03, happy_00
Row 4: happy_01, happy_02, happy_03, sad_00, sad_01
Row 5: sleep_00, sleep_01, sick_00, sick_01, sick_02
Row 6: sick_03, bathe_00, bathe_01, bathe_02, bathe_03

Critical rules:
- exactly these 30 frames only
- do not skip frames
- do not duplicate frames
- do not invent extra walk frames
- do not rename any frame
- labels must exactly match the frame names above
- keep all cells evenly sized
- keep the sprite centered above the label in each cell
- keep the board as a rigid contact-sheet grid
- do not turn the board into a poster or model sheet

Style:
- pixel art
- crisp edges
- slightly moody, cute, vulnerable desktop-pet tone
- fluffy tail essential, pointed ears, agile silhouette, clever face
- female should read slightly smaller and calmer
- red should be the dominant palette family without losing fox readability

Animation intent:
- idle: subtle breathing or blinking
- walk: readable side-view walk cycle
- eat: nibble or drink motion
- happy: excited positive motion
- sad: slumped low-energy pose
- sleep: clearly asleep and distinct from idle
- sick: visibly unwell and distinct from sad
- bathe: self-cleaning or washing motion

Return only the generated image.
```

## 32. fox / female / orange

```text
Generate an IMAGE ONLY.

Do not answer with text.
Do not describe limitations.
Do not provide configuration data.
Do not provide JSON.
Do not provide a table.
Do not provide a written explanation.

Create exactly one labeled sprite reference board image for this single variant only:

fox / female / orange

This prompt is self-contained. Do not rely on previous images.

Board rules:
- one single board only
- exactly 5 columns and exactly 6 rows
- exactly 30 total cells
- every row must contain exactly 5 cells
- every column must contain exactly 6 cells
- all cells must be equal size
- no merged cells
- no missing cells
- no extra cells
- no irregular row widths
- one sprite per cell
- one frame label at the bottom of each cell
- visible border lines between cells
- no title banner
- no extra decorations
- no extra animals
- no extra variants

Exact frame order:
Row 1: idle_00, idle_01, idle_02, idle_03, walk_00
Row 2: walk_01, walk_02, walk_03, walk_04, walk_05
Row 3: eat_00, eat_01, eat_02, eat_03, happy_00
Row 4: happy_01, happy_02, happy_03, sad_00, sad_01
Row 5: sleep_00, sleep_01, sick_00, sick_01, sick_02
Row 6: sick_03, bathe_00, bathe_01, bathe_02, bathe_03

Critical rules:
- exactly these 30 frames only
- do not skip frames
- do not duplicate frames
- do not invent extra walk frames
- do not rename any frame
- labels must exactly match the frame names above
- keep all cells evenly sized
- keep the sprite centered above the label in each cell
- keep the board as a rigid contact-sheet grid
- do not turn the board into a poster or model sheet

Style:
- pixel art
- crisp edges
- slightly moody, cute, vulnerable desktop-pet tone
- fluffy tail essential, pointed ears, agile silhouette, clever face
- female should read slightly smaller and calmer
- orange should be the dominant palette family without losing fox readability

Animation intent:
- idle: subtle breathing or blinking
- walk: readable side-view walk cycle
- eat: nibble or drink motion
- happy: excited positive motion
- sad: slumped low-energy pose
- sleep: clearly asleep and distinct from idle
- sick: visibly unwell and distinct from sad
- bathe: self-cleaning or washing motion

Return only the generated image.
```

## 33. fox / female / yellow

```text
Generate an IMAGE ONLY.

Do not answer with text.
Do not describe limitations.
Do not provide configuration data.
Do not provide JSON.
Do not provide a table.
Do not provide a written explanation.

Create exactly one labeled sprite reference board image for this single variant only:

fox / female / yellow

This prompt is self-contained. Do not rely on previous images.

Board rules:
- one single board only
- exactly 5 columns and exactly 6 rows
- exactly 30 total cells
- every row must contain exactly 5 cells
- every column must contain exactly 6 cells
- all cells must be equal size
- no merged cells
- no missing cells
- no extra cells
- no irregular row widths
- one sprite per cell
- one frame label at the bottom of each cell
- visible border lines between cells
- no title banner
- no extra decorations
- no extra animals
- no extra variants

Exact frame order:
Row 1: idle_00, idle_01, idle_02, idle_03, walk_00
Row 2: walk_01, walk_02, walk_03, walk_04, walk_05
Row 3: eat_00, eat_01, eat_02, eat_03, happy_00
Row 4: happy_01, happy_02, happy_03, sad_00, sad_01
Row 5: sleep_00, sleep_01, sick_00, sick_01, sick_02
Row 6: sick_03, bathe_00, bathe_01, bathe_02, bathe_03

Critical rules:
- exactly these 30 frames only
- do not skip frames
- do not duplicate frames
- do not invent extra walk frames
- do not rename any frame
- labels must exactly match the frame names above
- keep all cells evenly sized
- keep the sprite centered above the label in each cell
- keep the board as a rigid contact-sheet grid
- do not turn the board into a poster or model sheet

Style:
- pixel art
- crisp edges
- slightly moody, cute, vulnerable desktop-pet tone
- fluffy tail essential, pointed ears, agile silhouette, clever face
- female should read slightly smaller and calmer
- yellow should be the dominant palette family without losing fox readability

Animation intent:
- idle: subtle breathing or blinking
- walk: readable side-view walk cycle
- eat: nibble or drink motion
- happy: excited positive motion
- sad: slumped low-energy pose
- sleep: clearly asleep and distinct from idle
- sick: visibly unwell and distinct from sad
- bathe: self-cleaning or washing motion

Return only the generated image.
```

## 34. fox / female / blue

```text
Generate an IMAGE ONLY.

Do not answer with text.
Do not describe limitations.
Do not provide configuration data.
Do not provide JSON.
Do not provide a table.
Do not provide a written explanation.

Create exactly one labeled sprite reference board image for this single variant only:

fox / female / blue

This prompt is self-contained. Do not rely on previous images.

Board rules:
- one single board only
- exactly 5 columns and exactly 6 rows
- exactly 30 total cells
- every row must contain exactly 5 cells
- every column must contain exactly 6 cells
- all cells must be equal size
- no merged cells
- no missing cells
- no extra cells
- no irregular row widths
- one sprite per cell
- one frame label at the bottom of each cell
- visible border lines between cells
- no title banner
- no extra decorations
- no extra animals
- no extra variants

Exact frame order:
Row 1: idle_00, idle_01, idle_02, idle_03, walk_00
Row 2: walk_01, walk_02, walk_03, walk_04, walk_05
Row 3: eat_00, eat_01, eat_02, eat_03, happy_00
Row 4: happy_01, happy_02, happy_03, sad_00, sad_01
Row 5: sleep_00, sleep_01, sick_00, sick_01, sick_02
Row 6: sick_03, bathe_00, bathe_01, bathe_02, bathe_03

Critical rules:
- exactly these 30 frames only
- do not skip frames
- do not duplicate frames
- do not invent extra walk frames
- do not rename any frame
- labels must exactly match the frame names above
- keep all cells evenly sized
- keep the sprite centered above the label in each cell
- keep the board as a rigid contact-sheet grid
- do not turn the board into a poster or model sheet

Style:
- pixel art
- crisp edges
- slightly moody, cute, vulnerable desktop-pet tone
- fluffy tail essential, pointed ears, agile silhouette, clever face
- female should read slightly smaller and calmer
- blue should be the dominant palette family without losing fox readability

Animation intent:
- idle: subtle breathing or blinking
- walk: readable side-view walk cycle
- eat: nibble or drink motion
- happy: excited positive motion
- sad: slumped low-energy pose
- sleep: clearly asleep and distinct from idle
- sick: visibly unwell and distinct from sad
- bathe: self-cleaning or washing motion

Return only the generated image.
```

## 35. fox / female / indigo

```text
Generate an IMAGE ONLY.

Do not answer with text.
Do not describe limitations.
Do not provide configuration data.
Do not provide JSON.
Do not provide a table.
Do not provide a written explanation.

Create exactly one labeled sprite reference board image for this single variant only:

fox / female / indigo

This prompt is self-contained. Do not rely on previous images.

Board rules:
- one single board only
- exactly 5 columns and exactly 6 rows
- exactly 30 total cells
- every row must contain exactly 5 cells
- every column must contain exactly 6 cells
- all cells must be equal size
- no merged cells
- no missing cells
- no extra cells
- no irregular row widths
- one sprite per cell
- one frame label at the bottom of each cell
- visible border lines between cells
- no title banner
- no extra decorations
- no extra animals
- no extra variants

Exact frame order:
Row 1: idle_00, idle_01, idle_02, idle_03, walk_00
Row 2: walk_01, walk_02, walk_03, walk_04, walk_05
Row 3: eat_00, eat_01, eat_02, eat_03, happy_00
Row 4: happy_01, happy_02, happy_03, sad_00, sad_01
Row 5: sleep_00, sleep_01, sick_00, sick_01, sick_02
Row 6: sick_03, bathe_00, bathe_01, bathe_02, bathe_03

Critical rules:
- exactly these 30 frames only
- do not skip frames
- do not duplicate frames
- do not invent extra walk frames
- do not rename any frame
- labels must exactly match the frame names above
- keep all cells evenly sized
- keep the sprite centered above the label in each cell
- keep the board as a rigid contact-sheet grid
- do not turn the board into a poster or model sheet

Style:
- pixel art
- crisp edges
- slightly moody, cute, vulnerable desktop-pet tone
- fluffy tail essential, pointed ears, agile silhouette, clever face
- female should read slightly smaller and calmer
- indigo should be the dominant palette family without losing fox readability

Animation intent:
- idle: subtle breathing or blinking
- walk: readable side-view walk cycle
- eat: nibble or drink motion
- happy: excited positive motion
- sad: slumped low-energy pose
- sleep: clearly asleep and distinct from idle
- sick: visibly unwell and distinct from sad
- bathe: self-cleaning or washing motion

Return only the generated image.
```

## 36. fox / female / violet

```text
Generate an IMAGE ONLY.

Do not answer with text.
Do not describe limitations.
Do not provide configuration data.
Do not provide JSON.
Do not provide a table.
Do not provide a written explanation.

Create exactly one labeled sprite reference board image for this single variant only:

fox / female / violet

This prompt is self-contained. Do not rely on previous images.

Board rules:
- one single board only
- exactly 5 columns and exactly 6 rows
- exactly 30 total cells
- every row must contain exactly 5 cells
- every column must contain exactly 6 cells
- all cells must be equal size
- no merged cells
- no missing cells
- no extra cells
- no irregular row widths
- one sprite per cell
- one frame label at the bottom of each cell
- visible border lines between cells
- no title banner
- no extra decorations
- no extra animals
- no extra variants

Exact frame order:
Row 1: idle_00, idle_01, idle_02, idle_03, walk_00
Row 2: walk_01, walk_02, walk_03, walk_04, walk_05
Row 3: eat_00, eat_01, eat_02, eat_03, happy_00
Row 4: happy_01, happy_02, happy_03, sad_00, sad_01
Row 5: sleep_00, sleep_01, sick_00, sick_01, sick_02
Row 6: sick_03, bathe_00, bathe_01, bathe_02, bathe_03

Critical rules:
- exactly these 30 frames only
- do not skip frames
- do not duplicate frames
- do not invent extra walk frames
- do not rename any frame
- labels must exactly match the frame names above
- keep all cells evenly sized
- keep the sprite centered above the label in each cell
- keep the board as a rigid contact-sheet grid
- do not turn the board into a poster or model sheet

Style:
- pixel art
- crisp edges
- slightly moody, cute, vulnerable desktop-pet tone
- fluffy tail essential, pointed ears, agile silhouette, clever face
- female should read slightly smaller and calmer
- violet should be the dominant palette family without losing fox readability

Animation intent:
- idle: subtle breathing or blinking
- walk: readable side-view walk cycle
- eat: nibble or drink motion
- happy: excited positive motion
- sad: slumped low-energy pose
- sleep: clearly asleep and distinct from idle
- sick: visibly unwell and distinct from sad
- bathe: self-cleaning or washing motion

Return only the generated image.
```

## 37. snake / male / red

```text
Generate an IMAGE ONLY.

Do not answer with text.
Do not describe limitations.
Do not provide configuration data.
Do not provide JSON.
Do not provide a table.
Do not provide a written explanation.

Create exactly one labeled sprite reference board image for this single variant only:

snake / male / red

This prompt is self-contained. Do not rely on previous images.

Board rules:
- one single board only
- exactly 5 columns and exactly 6 rows
- exactly 30 total cells
- every row must contain exactly 5 cells
- every column must contain exactly 6 cells
- all cells must be equal size
- no merged cells
- no missing cells
- no extra cells
- no irregular row widths
- one sprite per cell
- one frame label at the bottom of each cell
- visible border lines between cells
- no title banner
- no extra decorations
- no extra animals
- no extra variants

Exact frame order:
Row 1: idle_00, idle_01, idle_02, idle_03, walk_00
Row 2: walk_01, walk_02, walk_03, walk_04, walk_05
Row 3: eat_00, eat_01, eat_02, eat_03, happy_00
Row 4: happy_01, happy_02, happy_03, sad_00, sad_01
Row 5: sleep_00, sleep_01, sick_00, sick_01, sick_02
Row 6: sick_03, bathe_00, bathe_01, bathe_02, bathe_03

Critical rules:
- exactly these 30 frames only
- do not skip frames
- do not duplicate frames
- do not invent extra walk frames
- do not rename any frame
- labels must exactly match the frame names above
- keep all cells evenly sized
- keep the sprite centered above the label in each cell
- keep the board as a rigid contact-sheet grid
- do not turn the board into a poster or model sheet

Style:
- pixel art
- crisp edges
- slightly moody, cute, vulnerable desktop-pet tone
- low slither silhouette, readable head shape, simple coiled or stretched poses, readable at small size
- male should read slightly larger and bolder
- red should be the dominant palette family without losing snake readability

Animation intent:
- idle: subtle breathing or blinking
- walk: readable side-view walk cycle
- eat: nibble or drink motion
- happy: excited positive motion
- sad: slumped low-energy pose
- sleep: clearly asleep and distinct from idle
- sick: visibly unwell and distinct from sad
- bathe: self-cleaning or washing motion

Return only the generated image.
```

## 38. snake / male / orange

```text
Generate an IMAGE ONLY.

Do not answer with text.
Do not describe limitations.
Do not provide configuration data.
Do not provide JSON.
Do not provide a table.
Do not provide a written explanation.

Create exactly one labeled sprite reference board image for this single variant only:

snake / male / orange

This prompt is self-contained. Do not rely on previous images.

Board rules:
- one single board only
- exactly 5 columns and exactly 6 rows
- exactly 30 total cells
- every row must contain exactly 5 cells
- every column must contain exactly 6 cells
- all cells must be equal size
- no merged cells
- no missing cells
- no extra cells
- no irregular row widths
- one sprite per cell
- one frame label at the bottom of each cell
- visible border lines between cells
- no title banner
- no extra decorations
- no extra animals
- no extra variants

Exact frame order:
Row 1: idle_00, idle_01, idle_02, idle_03, walk_00
Row 2: walk_01, walk_02, walk_03, walk_04, walk_05
Row 3: eat_00, eat_01, eat_02, eat_03, happy_00
Row 4: happy_01, happy_02, happy_03, sad_00, sad_01
Row 5: sleep_00, sleep_01, sick_00, sick_01, sick_02
Row 6: sick_03, bathe_00, bathe_01, bathe_02, bathe_03

Critical rules:
- exactly these 30 frames only
- do not skip frames
- do not duplicate frames
- do not invent extra walk frames
- do not rename any frame
- labels must exactly match the frame names above
- keep all cells evenly sized
- keep the sprite centered above the label in each cell
- keep the board as a rigid contact-sheet grid
- do not turn the board into a poster or model sheet

Style:
- pixel art
- crisp edges
- slightly moody, cute, vulnerable desktop-pet tone
- low slither silhouette, readable head shape, simple coiled or stretched poses, readable at small size
- male should read slightly larger and bolder
- orange should be the dominant palette family without losing snake readability

Animation intent:
- idle: subtle breathing or blinking
- walk: readable side-view walk cycle
- eat: nibble or drink motion
- happy: excited positive motion
- sad: slumped low-energy pose
- sleep: clearly asleep and distinct from idle
- sick: visibly unwell and distinct from sad
- bathe: self-cleaning or washing motion

Return only the generated image.
```

## 39. snake / male / yellow

```text
Generate an IMAGE ONLY.

Do not answer with text.
Do not describe limitations.
Do not provide configuration data.
Do not provide JSON.
Do not provide a table.
Do not provide a written explanation.

Create exactly one labeled sprite reference board image for this single variant only:

snake / male / yellow

This prompt is self-contained. Do not rely on previous images.

Board rules:
- one single board only
- exactly 5 columns and exactly 6 rows
- exactly 30 total cells
- every row must contain exactly 5 cells
- every column must contain exactly 6 cells
- all cells must be equal size
- no merged cells
- no missing cells
- no extra cells
- no irregular row widths
- one sprite per cell
- one frame label at the bottom of each cell
- visible border lines between cells
- no title banner
- no extra decorations
- no extra animals
- no extra variants

Exact frame order:
Row 1: idle_00, idle_01, idle_02, idle_03, walk_00
Row 2: walk_01, walk_02, walk_03, walk_04, walk_05
Row 3: eat_00, eat_01, eat_02, eat_03, happy_00
Row 4: happy_01, happy_02, happy_03, sad_00, sad_01
Row 5: sleep_00, sleep_01, sick_00, sick_01, sick_02
Row 6: sick_03, bathe_00, bathe_01, bathe_02, bathe_03

Critical rules:
- exactly these 30 frames only
- do not skip frames
- do not duplicate frames
- do not invent extra walk frames
- do not rename any frame
- labels must exactly match the frame names above
- keep all cells evenly sized
- keep the sprite centered above the label in each cell
- keep the board as a rigid contact-sheet grid
- do not turn the board into a poster or model sheet

Style:
- pixel art
- crisp edges
- slightly moody, cute, vulnerable desktop-pet tone
- low slither silhouette, readable head shape, simple coiled or stretched poses, readable at small size
- male should read slightly larger and bolder
- yellow should be the dominant palette family without losing snake readability

Animation intent:
- idle: subtle breathing or blinking
- walk: readable side-view walk cycle
- eat: nibble or drink motion
- happy: excited positive motion
- sad: slumped low-energy pose
- sleep: clearly asleep and distinct from idle
- sick: visibly unwell and distinct from sad
- bathe: self-cleaning or washing motion

Return only the generated image.
```

## 40. snake / male / blue

```text
Generate an IMAGE ONLY.

Do not answer with text.
Do not describe limitations.
Do not provide configuration data.
Do not provide JSON.
Do not provide a table.
Do not provide a written explanation.

Create exactly one labeled sprite reference board image for this single variant only:

snake / male / blue

This prompt is self-contained. Do not rely on previous images.

Board rules:
- one single board only
- exactly 5 columns and exactly 6 rows
- exactly 30 total cells
- every row must contain exactly 5 cells
- every column must contain exactly 6 cells
- all cells must be equal size
- no merged cells
- no missing cells
- no extra cells
- no irregular row widths
- one sprite per cell
- one frame label at the bottom of each cell
- visible border lines between cells
- no title banner
- no extra decorations
- no extra animals
- no extra variants

Exact frame order:
Row 1: idle_00, idle_01, idle_02, idle_03, walk_00
Row 2: walk_01, walk_02, walk_03, walk_04, walk_05
Row 3: eat_00, eat_01, eat_02, eat_03, happy_00
Row 4: happy_01, happy_02, happy_03, sad_00, sad_01
Row 5: sleep_00, sleep_01, sick_00, sick_01, sick_02
Row 6: sick_03, bathe_00, bathe_01, bathe_02, bathe_03

Critical rules:
- exactly these 30 frames only
- do not skip frames
- do not duplicate frames
- do not invent extra walk frames
- do not rename any frame
- labels must exactly match the frame names above
- keep all cells evenly sized
- keep the sprite centered above the label in each cell
- keep the board as a rigid contact-sheet grid
- do not turn the board into a poster or model sheet

Style:
- pixel art
- crisp edges
- slightly moody, cute, vulnerable desktop-pet tone
- low slither silhouette, readable head shape, simple coiled or stretched poses, readable at small size
- male should read slightly larger and bolder
- blue should be the dominant palette family without losing snake readability

Animation intent:
- idle: subtle breathing or blinking
- walk: readable side-view walk cycle
- eat: nibble or drink motion
- happy: excited positive motion
- sad: slumped low-energy pose
- sleep: clearly asleep and distinct from idle
- sick: visibly unwell and distinct from sad
- bathe: self-cleaning or washing motion

Return only the generated image.
```

## 41. snake / male / indigo

```text
Generate an IMAGE ONLY.

Do not answer with text.
Do not describe limitations.
Do not provide configuration data.
Do not provide JSON.
Do not provide a table.
Do not provide a written explanation.

Create exactly one labeled sprite reference board image for this single variant only:

snake / male / indigo

This prompt is self-contained. Do not rely on previous images.

Board rules:
- one single board only
- exactly 5 columns and exactly 6 rows
- exactly 30 total cells
- every row must contain exactly 5 cells
- every column must contain exactly 6 cells
- all cells must be equal size
- no merged cells
- no missing cells
- no extra cells
- no irregular row widths
- one sprite per cell
- one frame label at the bottom of each cell
- visible border lines between cells
- no title banner
- no extra decorations
- no extra animals
- no extra variants

Exact frame order:
Row 1: idle_00, idle_01, idle_02, idle_03, walk_00
Row 2: walk_01, walk_02, walk_03, walk_04, walk_05
Row 3: eat_00, eat_01, eat_02, eat_03, happy_00
Row 4: happy_01, happy_02, happy_03, sad_00, sad_01
Row 5: sleep_00, sleep_01, sick_00, sick_01, sick_02
Row 6: sick_03, bathe_00, bathe_01, bathe_02, bathe_03

Critical rules:
- exactly these 30 frames only
- do not skip frames
- do not duplicate frames
- do not invent extra walk frames
- do not rename any frame
- labels must exactly match the frame names above
- keep all cells evenly sized
- keep the sprite centered above the label in each cell
- keep the board as a rigid contact-sheet grid
- do not turn the board into a poster or model sheet

Style:
- pixel art
- crisp edges
- slightly moody, cute, vulnerable desktop-pet tone
- low slither silhouette, readable head shape, simple coiled or stretched poses, readable at small size
- male should read slightly larger and bolder
- indigo should be the dominant palette family without losing snake readability

Animation intent:
- idle: subtle breathing or blinking
- walk: readable side-view walk cycle
- eat: nibble or drink motion
- happy: excited positive motion
- sad: slumped low-energy pose
- sleep: clearly asleep and distinct from idle
- sick: visibly unwell and distinct from sad
- bathe: self-cleaning or washing motion

Return only the generated image.
```

## 42. snake / male / violet

```text
Generate an IMAGE ONLY.

Do not answer with text.
Do not describe limitations.
Do not provide configuration data.
Do not provide JSON.
Do not provide a table.
Do not provide a written explanation.

Create exactly one labeled sprite reference board image for this single variant only:

snake / male / violet

This prompt is self-contained. Do not rely on previous images.

Board rules:
- one single board only
- exactly 5 columns and exactly 6 rows
- exactly 30 total cells
- every row must contain exactly 5 cells
- every column must contain exactly 6 cells
- all cells must be equal size
- no merged cells
- no missing cells
- no extra cells
- no irregular row widths
- one sprite per cell
- one frame label at the bottom of each cell
- visible border lines between cells
- no title banner
- no extra decorations
- no extra animals
- no extra variants

Exact frame order:
Row 1: idle_00, idle_01, idle_02, idle_03, walk_00
Row 2: walk_01, walk_02, walk_03, walk_04, walk_05
Row 3: eat_00, eat_01, eat_02, eat_03, happy_00
Row 4: happy_01, happy_02, happy_03, sad_00, sad_01
Row 5: sleep_00, sleep_01, sick_00, sick_01, sick_02
Row 6: sick_03, bathe_00, bathe_01, bathe_02, bathe_03

Critical rules:
- exactly these 30 frames only
- do not skip frames
- do not duplicate frames
- do not invent extra walk frames
- do not rename any frame
- labels must exactly match the frame names above
- keep all cells evenly sized
- keep the sprite centered above the label in each cell
- keep the board as a rigid contact-sheet grid
- do not turn the board into a poster or model sheet

Style:
- pixel art
- crisp edges
- slightly moody, cute, vulnerable desktop-pet tone
- low slither silhouette, readable head shape, simple coiled or stretched poses, readable at small size
- male should read slightly larger and bolder
- violet should be the dominant palette family without losing snake readability

Animation intent:
- idle: subtle breathing or blinking
- walk: readable side-view walk cycle
- eat: nibble or drink motion
- happy: excited positive motion
- sad: slumped low-energy pose
- sleep: clearly asleep and distinct from idle
- sick: visibly unwell and distinct from sad
- bathe: self-cleaning or washing motion

Return only the generated image.
```

## 43. snake / female / red

```text
Generate an IMAGE ONLY.

Do not answer with text.
Do not describe limitations.
Do not provide configuration data.
Do not provide JSON.
Do not provide a table.
Do not provide a written explanation.

Create exactly one labeled sprite reference board image for this single variant only:

snake / female / red

This prompt is self-contained. Do not rely on previous images.

Board rules:
- one single board only
- exactly 5 columns and exactly 6 rows
- exactly 30 total cells
- every row must contain exactly 5 cells
- every column must contain exactly 6 cells
- all cells must be equal size
- no merged cells
- no missing cells
- no extra cells
- no irregular row widths
- one sprite per cell
- one frame label at the bottom of each cell
- visible border lines between cells
- no title banner
- no extra decorations
- no extra animals
- no extra variants

Exact frame order:
Row 1: idle_00, idle_01, idle_02, idle_03, walk_00
Row 2: walk_01, walk_02, walk_03, walk_04, walk_05
Row 3: eat_00, eat_01, eat_02, eat_03, happy_00
Row 4: happy_01, happy_02, happy_03, sad_00, sad_01
Row 5: sleep_00, sleep_01, sick_00, sick_01, sick_02
Row 6: sick_03, bathe_00, bathe_01, bathe_02, bathe_03

Critical rules:
- exactly these 30 frames only
- do not skip frames
- do not duplicate frames
- do not invent extra walk frames
- do not rename any frame
- labels must exactly match the frame names above
- keep all cells evenly sized
- keep the sprite centered above the label in each cell
- keep the board as a rigid contact-sheet grid
- do not turn the board into a poster or model sheet

Style:
- pixel art
- crisp edges
- slightly moody, cute, vulnerable desktop-pet tone
- low slither silhouette, readable head shape, simple coiled or stretched poses, readable at small size
- female should read slightly smaller and calmer
- red should be the dominant palette family without losing snake readability

Animation intent:
- idle: subtle breathing or blinking
- walk: readable side-view walk cycle
- eat: nibble or drink motion
- happy: excited positive motion
- sad: slumped low-energy pose
- sleep: clearly asleep and distinct from idle
- sick: visibly unwell and distinct from sad
- bathe: self-cleaning or washing motion

Return only the generated image.
```

## 44. snake / female / orange

```text
Generate an IMAGE ONLY.

Do not answer with text.
Do not describe limitations.
Do not provide configuration data.
Do not provide JSON.
Do not provide a table.
Do not provide a written explanation.

Create exactly one labeled sprite reference board image for this single variant only:

snake / female / orange

This prompt is self-contained. Do not rely on previous images.

Board rules:
- one single board only
- exactly 5 columns and exactly 6 rows
- exactly 30 total cells
- every row must contain exactly 5 cells
- every column must contain exactly 6 cells
- all cells must be equal size
- no merged cells
- no missing cells
- no extra cells
- no irregular row widths
- one sprite per cell
- one frame label at the bottom of each cell
- visible border lines between cells
- no title banner
- no extra decorations
- no extra animals
- no extra variants

Exact frame order:
Row 1: idle_00, idle_01, idle_02, idle_03, walk_00
Row 2: walk_01, walk_02, walk_03, walk_04, walk_05
Row 3: eat_00, eat_01, eat_02, eat_03, happy_00
Row 4: happy_01, happy_02, happy_03, sad_00, sad_01
Row 5: sleep_00, sleep_01, sick_00, sick_01, sick_02
Row 6: sick_03, bathe_00, bathe_01, bathe_02, bathe_03

Critical rules:
- exactly these 30 frames only
- do not skip frames
- do not duplicate frames
- do not invent extra walk frames
- do not rename any frame
- labels must exactly match the frame names above
- keep all cells evenly sized
- keep the sprite centered above the label in each cell
- keep the board as a rigid contact-sheet grid
- do not turn the board into a poster or model sheet

Style:
- pixel art
- crisp edges
- slightly moody, cute, vulnerable desktop-pet tone
- low slither silhouette, readable head shape, simple coiled or stretched poses, readable at small size
- female should read slightly smaller and calmer
- orange should be the dominant palette family without losing snake readability

Animation intent:
- idle: subtle breathing or blinking
- walk: readable side-view walk cycle
- eat: nibble or drink motion
- happy: excited positive motion
- sad: slumped low-energy pose
- sleep: clearly asleep and distinct from idle
- sick: visibly unwell and distinct from sad
- bathe: self-cleaning or washing motion

Return only the generated image.
```

## 45. snake / female / yellow

```text
Generate an IMAGE ONLY.

Do not answer with text.
Do not describe limitations.
Do not provide configuration data.
Do not provide JSON.
Do not provide a table.
Do not provide a written explanation.

Create exactly one labeled sprite reference board image for this single variant only:

snake / female / yellow

This prompt is self-contained. Do not rely on previous images.

Board rules:
- one single board only
- exactly 5 columns and exactly 6 rows
- exactly 30 total cells
- every row must contain exactly 5 cells
- every column must contain exactly 6 cells
- all cells must be equal size
- no merged cells
- no missing cells
- no extra cells
- no irregular row widths
- one sprite per cell
- one frame label at the bottom of each cell
- visible border lines between cells
- no title banner
- no extra decorations
- no extra animals
- no extra variants

Exact frame order:
Row 1: idle_00, idle_01, idle_02, idle_03, walk_00
Row 2: walk_01, walk_02, walk_03, walk_04, walk_05
Row 3: eat_00, eat_01, eat_02, eat_03, happy_00
Row 4: happy_01, happy_02, happy_03, sad_00, sad_01
Row 5: sleep_00, sleep_01, sick_00, sick_01, sick_02
Row 6: sick_03, bathe_00, bathe_01, bathe_02, bathe_03

Critical rules:
- exactly these 30 frames only
- do not skip frames
- do not duplicate frames
- do not invent extra walk frames
- do not rename any frame
- labels must exactly match the frame names above
- keep all cells evenly sized
- keep the sprite centered above the label in each cell
- keep the board as a rigid contact-sheet grid
- do not turn the board into a poster or model sheet

Style:
- pixel art
- crisp edges
- slightly moody, cute, vulnerable desktop-pet tone
- low slither silhouette, readable head shape, simple coiled or stretched poses, readable at small size
- female should read slightly smaller and calmer
- yellow should be the dominant palette family without losing snake readability

Animation intent:
- idle: subtle breathing or blinking
- walk: readable side-view walk cycle
- eat: nibble or drink motion
- happy: excited positive motion
- sad: slumped low-energy pose
- sleep: clearly asleep and distinct from idle
- sick: visibly unwell and distinct from sad
- bathe: self-cleaning or washing motion

Return only the generated image.
```

## 46. snake / female / blue

```text
Generate an IMAGE ONLY.

Do not answer with text.
Do not describe limitations.
Do not provide configuration data.
Do not provide JSON.
Do not provide a table.
Do not provide a written explanation.

Create exactly one labeled sprite reference board image for this single variant only:

snake / female / blue

This prompt is self-contained. Do not rely on previous images.

Board rules:
- one single board only
- exactly 5 columns and exactly 6 rows
- exactly 30 total cells
- every row must contain exactly 5 cells
- every column must contain exactly 6 cells
- all cells must be equal size
- no merged cells
- no missing cells
- no extra cells
- no irregular row widths
- one sprite per cell
- one frame label at the bottom of each cell
- visible border lines between cells
- no title banner
- no extra decorations
- no extra animals
- no extra variants

Exact frame order:
Row 1: idle_00, idle_01, idle_02, idle_03, walk_00
Row 2: walk_01, walk_02, walk_03, walk_04, walk_05
Row 3: eat_00, eat_01, eat_02, eat_03, happy_00
Row 4: happy_01, happy_02, happy_03, sad_00, sad_01
Row 5: sleep_00, sleep_01, sick_00, sick_01, sick_02
Row 6: sick_03, bathe_00, bathe_01, bathe_02, bathe_03

Critical rules:
- exactly these 30 frames only
- do not skip frames
- do not duplicate frames
- do not invent extra walk frames
- do not rename any frame
- labels must exactly match the frame names above
- keep all cells evenly sized
- keep the sprite centered above the label in each cell
- keep the board as a rigid contact-sheet grid
- do not turn the board into a poster or model sheet

Style:
- pixel art
- crisp edges
- slightly moody, cute, vulnerable desktop-pet tone
- low slither silhouette, readable head shape, simple coiled or stretched poses, readable at small size
- female should read slightly smaller and calmer
- blue should be the dominant palette family without losing snake readability

Animation intent:
- idle: subtle breathing or blinking
- walk: readable side-view walk cycle
- eat: nibble or drink motion
- happy: excited positive motion
- sad: slumped low-energy pose
- sleep: clearly asleep and distinct from idle
- sick: visibly unwell and distinct from sad
- bathe: self-cleaning or washing motion

Return only the generated image.
```

## 47. snake / female / indigo

```text
Generate an IMAGE ONLY.

Do not answer with text.
Do not describe limitations.
Do not provide configuration data.
Do not provide JSON.
Do not provide a table.
Do not provide a written explanation.

Create exactly one labeled sprite reference board image for this single variant only:

snake / female / indigo

This prompt is self-contained. Do not rely on previous images.

Board rules:
- one single board only
- exactly 5 columns and exactly 6 rows
- exactly 30 total cells
- every row must contain exactly 5 cells
- every column must contain exactly 6 cells
- all cells must be equal size
- no merged cells
- no missing cells
- no extra cells
- no irregular row widths
- one sprite per cell
- one frame label at the bottom of each cell
- visible border lines between cells
- no title banner
- no extra decorations
- no extra animals
- no extra variants

Exact frame order:
Row 1: idle_00, idle_01, idle_02, idle_03, walk_00
Row 2: walk_01, walk_02, walk_03, walk_04, walk_05
Row 3: eat_00, eat_01, eat_02, eat_03, happy_00
Row 4: happy_01, happy_02, happy_03, sad_00, sad_01
Row 5: sleep_00, sleep_01, sick_00, sick_01, sick_02
Row 6: sick_03, bathe_00, bathe_01, bathe_02, bathe_03

Critical rules:
- exactly these 30 frames only
- do not skip frames
- do not duplicate frames
- do not invent extra walk frames
- do not rename any frame
- labels must exactly match the frame names above
- keep all cells evenly sized
- keep the sprite centered above the label in each cell
- keep the board as a rigid contact-sheet grid
- do not turn the board into a poster or model sheet

Style:
- pixel art
- crisp edges
- slightly moody, cute, vulnerable desktop-pet tone
- low slither silhouette, readable head shape, simple coiled or stretched poses, readable at small size
- female should read slightly smaller and calmer
- indigo should be the dominant palette family without losing snake readability

Animation intent:
- idle: subtle breathing or blinking
- walk: readable side-view walk cycle
- eat: nibble or drink motion
- happy: excited positive motion
- sad: slumped low-energy pose
- sleep: clearly asleep and distinct from idle
- sick: visibly unwell and distinct from sad
- bathe: self-cleaning or washing motion

Return only the generated image.
```

## 48. snake / female / violet

```text
Generate an IMAGE ONLY.

Do not answer with text.
Do not describe limitations.
Do not provide configuration data.
Do not provide JSON.
Do not provide a table.
Do not provide a written explanation.

Create exactly one labeled sprite reference board image for this single variant only:

snake / female / violet

This prompt is self-contained. Do not rely on previous images.

Board rules:
- one single board only
- exactly 5 columns and exactly 6 rows
- exactly 30 total cells
- every row must contain exactly 5 cells
- every column must contain exactly 6 cells
- all cells must be equal size
- no merged cells
- no missing cells
- no extra cells
- no irregular row widths
- one sprite per cell
- one frame label at the bottom of each cell
- visible border lines between cells
- no title banner
- no extra decorations
- no extra animals
- no extra variants

Exact frame order:
Row 1: idle_00, idle_01, idle_02, idle_03, walk_00
Row 2: walk_01, walk_02, walk_03, walk_04, walk_05
Row 3: eat_00, eat_01, eat_02, eat_03, happy_00
Row 4: happy_01, happy_02, happy_03, sad_00, sad_01
Row 5: sleep_00, sleep_01, sick_00, sick_01, sick_02
Row 6: sick_03, bathe_00, bathe_01, bathe_02, bathe_03

Critical rules:
- exactly these 30 frames only
- do not skip frames
- do not duplicate frames
- do not invent extra walk frames
- do not rename any frame
- labels must exactly match the frame names above
- keep all cells evenly sized
- keep the sprite centered above the label in each cell
- keep the board as a rigid contact-sheet grid
- do not turn the board into a poster or model sheet

Style:
- pixel art
- crisp edges
- slightly moody, cute, vulnerable desktop-pet tone
- low slither silhouette, readable head shape, simple coiled or stretched poses, readable at small size
- female should read slightly smaller and calmer
- violet should be the dominant palette family without losing snake readability

Animation intent:
- idle: subtle breathing or blinking
- walk: readable side-view walk cycle
- eat: nibble or drink motion
- happy: excited positive motion
- sad: slumped low-energy pose
- sleep: clearly asleep and distinct from idle
- sick: visibly unwell and distinct from sad
- bathe: self-cleaning or washing motion

Return only the generated image.
```

## 49. deer / male / red

```text
Generate an IMAGE ONLY.

Do not answer with text.
Do not describe limitations.
Do not provide configuration data.
Do not provide JSON.
Do not provide a table.
Do not provide a written explanation.

Create exactly one labeled sprite reference board image for this single variant only:

deer / male / red

This prompt is self-contained. Do not rely on previous images.

Board rules:
- one single board only
- exactly 5 columns and exactly 6 rows
- exactly 30 total cells
- every row must contain exactly 5 cells
- every column must contain exactly 6 cells
- all cells must be equal size
- no merged cells
- no missing cells
- no extra cells
- no irregular row widths
- one sprite per cell
- one frame label at the bottom of each cell
- visible border lines between cells
- no title banner
- no extra decorations
- no extra animals
- no extra variants

Exact frame order:
Row 1: idle_00, idle_01, idle_02, idle_03, walk_00
Row 2: walk_01, walk_02, walk_03, walk_04, walk_05
Row 3: eat_00, eat_01, eat_02, eat_03, happy_00
Row 4: happy_01, happy_02, happy_03, sad_00, sad_01
Row 5: sleep_00, sleep_01, sick_00, sick_01, sick_02
Row 6: sick_03, bathe_00, bathe_01, bathe_02, bathe_03

Critical rules:
- exactly these 30 frames only
- do not skip frames
- do not duplicate frames
- do not invent extra walk frames
- do not rename any frame
- labels must exactly match the frame names above
- keep all cells evenly sized
- keep the sprite centered above the label in each cell
- keep the board as a rigid contact-sheet grid
- do not turn the board into a poster or model sheet

Style:
- pixel art
- crisp edges
- slightly moody, cute, vulnerable desktop-pet tone
- delicate legs, elegant face and ears, gentle posture, subtle antlers only if readable
- male should read slightly larger and bolder
- red should be the dominant palette family without losing deer readability

Animation intent:
- idle: subtle breathing or blinking
- walk: readable side-view walk cycle
- eat: nibble or drink motion
- happy: excited positive motion
- sad: slumped low-energy pose
- sleep: clearly asleep and distinct from idle
- sick: visibly unwell and distinct from sad
- bathe: self-cleaning or washing motion

Return only the generated image.
```

## 50. deer / male / orange

```text
Generate an IMAGE ONLY.

Do not answer with text.
Do not describe limitations.
Do not provide configuration data.
Do not provide JSON.
Do not provide a table.
Do not provide a written explanation.

Create exactly one labeled sprite reference board image for this single variant only:

deer / male / orange

This prompt is self-contained. Do not rely on previous images.

Board rules:
- one single board only
- exactly 5 columns and exactly 6 rows
- exactly 30 total cells
- every row must contain exactly 5 cells
- every column must contain exactly 6 cells
- all cells must be equal size
- no merged cells
- no missing cells
- no extra cells
- no irregular row widths
- one sprite per cell
- one frame label at the bottom of each cell
- visible border lines between cells
- no title banner
- no extra decorations
- no extra animals
- no extra variants

Exact frame order:
Row 1: idle_00, idle_01, idle_02, idle_03, walk_00
Row 2: walk_01, walk_02, walk_03, walk_04, walk_05
Row 3: eat_00, eat_01, eat_02, eat_03, happy_00
Row 4: happy_01, happy_02, happy_03, sad_00, sad_01
Row 5: sleep_00, sleep_01, sick_00, sick_01, sick_02
Row 6: sick_03, bathe_00, bathe_01, bathe_02, bathe_03

Critical rules:
- exactly these 30 frames only
- do not skip frames
- do not duplicate frames
- do not invent extra walk frames
- do not rename any frame
- labels must exactly match the frame names above
- keep all cells evenly sized
- keep the sprite centered above the label in each cell
- keep the board as a rigid contact-sheet grid
- do not turn the board into a poster or model sheet

Style:
- pixel art
- crisp edges
- slightly moody, cute, vulnerable desktop-pet tone
- delicate legs, elegant face and ears, gentle posture, subtle antlers only if readable
- male should read slightly larger and bolder
- orange should be the dominant palette family without losing deer readability

Animation intent:
- idle: subtle breathing or blinking
- walk: readable side-view walk cycle
- eat: nibble or drink motion
- happy: excited positive motion
- sad: slumped low-energy pose
- sleep: clearly asleep and distinct from idle
- sick: visibly unwell and distinct from sad
- bathe: self-cleaning or washing motion

Return only the generated image.
```

## 51. deer / male / yellow

```text
Generate an IMAGE ONLY.

Do not answer with text.
Do not describe limitations.
Do not provide configuration data.
Do not provide JSON.
Do not provide a table.
Do not provide a written explanation.

Create exactly one labeled sprite reference board image for this single variant only:

deer / male / yellow

This prompt is self-contained. Do not rely on previous images.

Board rules:
- one single board only
- exactly 5 columns and exactly 6 rows
- exactly 30 total cells
- every row must contain exactly 5 cells
- every column must contain exactly 6 cells
- all cells must be equal size
- no merged cells
- no missing cells
- no extra cells
- no irregular row widths
- one sprite per cell
- one frame label at the bottom of each cell
- visible border lines between cells
- no title banner
- no extra decorations
- no extra animals
- no extra variants

Exact frame order:
Row 1: idle_00, idle_01, idle_02, idle_03, walk_00
Row 2: walk_01, walk_02, walk_03, walk_04, walk_05
Row 3: eat_00, eat_01, eat_02, eat_03, happy_00
Row 4: happy_01, happy_02, happy_03, sad_00, sad_01
Row 5: sleep_00, sleep_01, sick_00, sick_01, sick_02
Row 6: sick_03, bathe_00, bathe_01, bathe_02, bathe_03

Critical rules:
- exactly these 30 frames only
- do not skip frames
- do not duplicate frames
- do not invent extra walk frames
- do not rename any frame
- labels must exactly match the frame names above
- keep all cells evenly sized
- keep the sprite centered above the label in each cell
- keep the board as a rigid contact-sheet grid
- do not turn the board into a poster or model sheet

Style:
- pixel art
- crisp edges
- slightly moody, cute, vulnerable desktop-pet tone
- delicate legs, elegant face and ears, gentle posture, subtle antlers only if readable
- male should read slightly larger and bolder
- yellow should be the dominant palette family without losing deer readability

Animation intent:
- idle: subtle breathing or blinking
- walk: readable side-view walk cycle
- eat: nibble or drink motion
- happy: excited positive motion
- sad: slumped low-energy pose
- sleep: clearly asleep and distinct from idle
- sick: visibly unwell and distinct from sad
- bathe: self-cleaning or washing motion

Return only the generated image.
```

## 52. deer / male / blue

```text
Generate an IMAGE ONLY.

Do not answer with text.
Do not describe limitations.
Do not provide configuration data.
Do not provide JSON.
Do not provide a table.
Do not provide a written explanation.

Create exactly one labeled sprite reference board image for this single variant only:

deer / male / blue

This prompt is self-contained. Do not rely on previous images.

Board rules:
- one single board only
- exactly 5 columns and exactly 6 rows
- exactly 30 total cells
- every row must contain exactly 5 cells
- every column must contain exactly 6 cells
- all cells must be equal size
- no merged cells
- no missing cells
- no extra cells
- no irregular row widths
- one sprite per cell
- one frame label at the bottom of each cell
- visible border lines between cells
- no title banner
- no extra decorations
- no extra animals
- no extra variants

Exact frame order:
Row 1: idle_00, idle_01, idle_02, idle_03, walk_00
Row 2: walk_01, walk_02, walk_03, walk_04, walk_05
Row 3: eat_00, eat_01, eat_02, eat_03, happy_00
Row 4: happy_01, happy_02, happy_03, sad_00, sad_01
Row 5: sleep_00, sleep_01, sick_00, sick_01, sick_02
Row 6: sick_03, bathe_00, bathe_01, bathe_02, bathe_03

Critical rules:
- exactly these 30 frames only
- do not skip frames
- do not duplicate frames
- do not invent extra walk frames
- do not rename any frame
- labels must exactly match the frame names above
- keep all cells evenly sized
- keep the sprite centered above the label in each cell
- keep the board as a rigid contact-sheet grid
- do not turn the board into a poster or model sheet

Style:
- pixel art
- crisp edges
- slightly moody, cute, vulnerable desktop-pet tone
- delicate legs, elegant face and ears, gentle posture, subtle antlers only if readable
- male should read slightly larger and bolder
- blue should be the dominant palette family without losing deer readability

Animation intent:
- idle: subtle breathing or blinking
- walk: readable side-view walk cycle
- eat: nibble or drink motion
- happy: excited positive motion
- sad: slumped low-energy pose
- sleep: clearly asleep and distinct from idle
- sick: visibly unwell and distinct from sad
- bathe: self-cleaning or washing motion

Return only the generated image.
```

## 53. deer / male / indigo

```text
Generate an IMAGE ONLY.

Do not answer with text.
Do not describe limitations.
Do not provide configuration data.
Do not provide JSON.
Do not provide a table.
Do not provide a written explanation.

Create exactly one labeled sprite reference board image for this single variant only:

deer / male / indigo

This prompt is self-contained. Do not rely on previous images.

Board rules:
- one single board only
- exactly 5 columns and exactly 6 rows
- exactly 30 total cells
- every row must contain exactly 5 cells
- every column must contain exactly 6 cells
- all cells must be equal size
- no merged cells
- no missing cells
- no extra cells
- no irregular row widths
- one sprite per cell
- one frame label at the bottom of each cell
- visible border lines between cells
- no title banner
- no extra decorations
- no extra animals
- no extra variants

Exact frame order:
Row 1: idle_00, idle_01, idle_02, idle_03, walk_00
Row 2: walk_01, walk_02, walk_03, walk_04, walk_05
Row 3: eat_00, eat_01, eat_02, eat_03, happy_00
Row 4: happy_01, happy_02, happy_03, sad_00, sad_01
Row 5: sleep_00, sleep_01, sick_00, sick_01, sick_02
Row 6: sick_03, bathe_00, bathe_01, bathe_02, bathe_03

Critical rules:
- exactly these 30 frames only
- do not skip frames
- do not duplicate frames
- do not invent extra walk frames
- do not rename any frame
- labels must exactly match the frame names above
- keep all cells evenly sized
- keep the sprite centered above the label in each cell
- keep the board as a rigid contact-sheet grid
- do not turn the board into a poster or model sheet

Style:
- pixel art
- crisp edges
- slightly moody, cute, vulnerable desktop-pet tone
- delicate legs, elegant face and ears, gentle posture, subtle antlers only if readable
- male should read slightly larger and bolder
- indigo should be the dominant palette family without losing deer readability

Animation intent:
- idle: subtle breathing or blinking
- walk: readable side-view walk cycle
- eat: nibble or drink motion
- happy: excited positive motion
- sad: slumped low-energy pose
- sleep: clearly asleep and distinct from idle
- sick: visibly unwell and distinct from sad
- bathe: self-cleaning or washing motion

Return only the generated image.
```

## 54. deer / male / violet

```text
Generate an IMAGE ONLY.

Do not answer with text.
Do not describe limitations.
Do not provide configuration data.
Do not provide JSON.
Do not provide a table.
Do not provide a written explanation.

Create exactly one labeled sprite reference board image for this single variant only:

deer / male / violet

This prompt is self-contained. Do not rely on previous images.

Board rules:
- one single board only
- exactly 5 columns and exactly 6 rows
- exactly 30 total cells
- every row must contain exactly 5 cells
- every column must contain exactly 6 cells
- all cells must be equal size
- no merged cells
- no missing cells
- no extra cells
- no irregular row widths
- one sprite per cell
- one frame label at the bottom of each cell
- visible border lines between cells
- no title banner
- no extra decorations
- no extra animals
- no extra variants

Exact frame order:
Row 1: idle_00, idle_01, idle_02, idle_03, walk_00
Row 2: walk_01, walk_02, walk_03, walk_04, walk_05
Row 3: eat_00, eat_01, eat_02, eat_03, happy_00
Row 4: happy_01, happy_02, happy_03, sad_00, sad_01
Row 5: sleep_00, sleep_01, sick_00, sick_01, sick_02
Row 6: sick_03, bathe_00, bathe_01, bathe_02, bathe_03

Critical rules:
- exactly these 30 frames only
- do not skip frames
- do not duplicate frames
- do not invent extra walk frames
- do not rename any frame
- labels must exactly match the frame names above
- keep all cells evenly sized
- keep the sprite centered above the label in each cell
- keep the board as a rigid contact-sheet grid
- do not turn the board into a poster or model sheet

Style:
- pixel art
- crisp edges
- slightly moody, cute, vulnerable desktop-pet tone
- delicate legs, elegant face and ears, gentle posture, subtle antlers only if readable
- male should read slightly larger and bolder
- violet should be the dominant palette family without losing deer readability

Animation intent:
- idle: subtle breathing or blinking
- walk: readable side-view walk cycle
- eat: nibble or drink motion
- happy: excited positive motion
- sad: slumped low-energy pose
- sleep: clearly asleep and distinct from idle
- sick: visibly unwell and distinct from sad
- bathe: self-cleaning or washing motion

Return only the generated image.
```

## 55. deer / female / red

```text
Generate an IMAGE ONLY.

Do not answer with text.
Do not describe limitations.
Do not provide configuration data.
Do not provide JSON.
Do not provide a table.
Do not provide a written explanation.

Create exactly one labeled sprite reference board image for this single variant only:

deer / female / red

This prompt is self-contained. Do not rely on previous images.

Board rules:
- one single board only
- exactly 5 columns and exactly 6 rows
- exactly 30 total cells
- every row must contain exactly 5 cells
- every column must contain exactly 6 cells
- all cells must be equal size
- no merged cells
- no missing cells
- no extra cells
- no irregular row widths
- one sprite per cell
- one frame label at the bottom of each cell
- visible border lines between cells
- no title banner
- no extra decorations
- no extra animals
- no extra variants

Exact frame order:
Row 1: idle_00, idle_01, idle_02, idle_03, walk_00
Row 2: walk_01, walk_02, walk_03, walk_04, walk_05
Row 3: eat_00, eat_01, eat_02, eat_03, happy_00
Row 4: happy_01, happy_02, happy_03, sad_00, sad_01
Row 5: sleep_00, sleep_01, sick_00, sick_01, sick_02
Row 6: sick_03, bathe_00, bathe_01, bathe_02, bathe_03

Critical rules:
- exactly these 30 frames only
- do not skip frames
- do not duplicate frames
- do not invent extra walk frames
- do not rename any frame
- labels must exactly match the frame names above
- keep all cells evenly sized
- keep the sprite centered above the label in each cell
- keep the board as a rigid contact-sheet grid
- do not turn the board into a poster or model sheet

Style:
- pixel art
- crisp edges
- slightly moody, cute, vulnerable desktop-pet tone
- delicate legs, elegant face and ears, gentle posture, subtle antlers only if readable
- female should read slightly smaller and calmer
- red should be the dominant palette family without losing deer readability

Animation intent:
- idle: subtle breathing or blinking
- walk: readable side-view walk cycle
- eat: nibble or drink motion
- happy: excited positive motion
- sad: slumped low-energy pose
- sleep: clearly asleep and distinct from idle
- sick: visibly unwell and distinct from sad
- bathe: self-cleaning or washing motion

Return only the generated image.
```

## 56. deer / female / orange

```text
Generate an IMAGE ONLY.

Do not answer with text.
Do not describe limitations.
Do not provide configuration data.
Do not provide JSON.
Do not provide a table.
Do not provide a written explanation.

Create exactly one labeled sprite reference board image for this single variant only:

deer / female / orange

This prompt is self-contained. Do not rely on previous images.

Board rules:
- one single board only
- exactly 5 columns and exactly 6 rows
- exactly 30 total cells
- every row must contain exactly 5 cells
- every column must contain exactly 6 cells
- all cells must be equal size
- no merged cells
- no missing cells
- no extra cells
- no irregular row widths
- one sprite per cell
- one frame label at the bottom of each cell
- visible border lines between cells
- no title banner
- no extra decorations
- no extra animals
- no extra variants

Exact frame order:
Row 1: idle_00, idle_01, idle_02, idle_03, walk_00
Row 2: walk_01, walk_02, walk_03, walk_04, walk_05
Row 3: eat_00, eat_01, eat_02, eat_03, happy_00
Row 4: happy_01, happy_02, happy_03, sad_00, sad_01
Row 5: sleep_00, sleep_01, sick_00, sick_01, sick_02
Row 6: sick_03, bathe_00, bathe_01, bathe_02, bathe_03

Critical rules:
- exactly these 30 frames only
- do not skip frames
- do not duplicate frames
- do not invent extra walk frames
- do not rename any frame
- labels must exactly match the frame names above
- keep all cells evenly sized
- keep the sprite centered above the label in each cell
- keep the board as a rigid contact-sheet grid
- do not turn the board into a poster or model sheet

Style:
- pixel art
- crisp edges
- slightly moody, cute, vulnerable desktop-pet tone
- delicate legs, elegant face and ears, gentle posture, subtle antlers only if readable
- female should read slightly smaller and calmer
- orange should be the dominant palette family without losing deer readability

Animation intent:
- idle: subtle breathing or blinking
- walk: readable side-view walk cycle
- eat: nibble or drink motion
- happy: excited positive motion
- sad: slumped low-energy pose
- sleep: clearly asleep and distinct from idle
- sick: visibly unwell and distinct from sad
- bathe: self-cleaning or washing motion

Return only the generated image.
```

## 57. deer / female / yellow

```text
Generate an IMAGE ONLY.

Do not answer with text.
Do not describe limitations.
Do not provide configuration data.
Do not provide JSON.
Do not provide a table.
Do not provide a written explanation.

Create exactly one labeled sprite reference board image for this single variant only:

deer / female / yellow

This prompt is self-contained. Do not rely on previous images.

Board rules:
- one single board only
- exactly 5 columns and exactly 6 rows
- exactly 30 total cells
- every row must contain exactly 5 cells
- every column must contain exactly 6 cells
- all cells must be equal size
- no merged cells
- no missing cells
- no extra cells
- no irregular row widths
- one sprite per cell
- one frame label at the bottom of each cell
- visible border lines between cells
- no title banner
- no extra decorations
- no extra animals
- no extra variants

Exact frame order:
Row 1: idle_00, idle_01, idle_02, idle_03, walk_00
Row 2: walk_01, walk_02, walk_03, walk_04, walk_05
Row 3: eat_00, eat_01, eat_02, eat_03, happy_00
Row 4: happy_01, happy_02, happy_03, sad_00, sad_01
Row 5: sleep_00, sleep_01, sick_00, sick_01, sick_02
Row 6: sick_03, bathe_00, bathe_01, bathe_02, bathe_03

Critical rules:
- exactly these 30 frames only
- do not skip frames
- do not duplicate frames
- do not invent extra walk frames
- do not rename any frame
- labels must exactly match the frame names above
- keep all cells evenly sized
- keep the sprite centered above the label in each cell
- keep the board as a rigid contact-sheet grid
- do not turn the board into a poster or model sheet

Style:
- pixel art
- crisp edges
- slightly moody, cute, vulnerable desktop-pet tone
- delicate legs, elegant face and ears, gentle posture, subtle antlers only if readable
- female should read slightly smaller and calmer
- yellow should be the dominant palette family without losing deer readability

Animation intent:
- idle: subtle breathing or blinking
- walk: readable side-view walk cycle
- eat: nibble or drink motion
- happy: excited positive motion
- sad: slumped low-energy pose
- sleep: clearly asleep and distinct from idle
- sick: visibly unwell and distinct from sad
- bathe: self-cleaning or washing motion

Return only the generated image.
```

## 58. deer / female / blue

```text
Generate an IMAGE ONLY.

Do not answer with text.
Do not describe limitations.
Do not provide configuration data.
Do not provide JSON.
Do not provide a table.
Do not provide a written explanation.

Create exactly one labeled sprite reference board image for this single variant only:

deer / female / blue

This prompt is self-contained. Do not rely on previous images.

Board rules:
- one single board only
- exactly 5 columns and exactly 6 rows
- exactly 30 total cells
- every row must contain exactly 5 cells
- every column must contain exactly 6 cells
- all cells must be equal size
- no merged cells
- no missing cells
- no extra cells
- no irregular row widths
- one sprite per cell
- one frame label at the bottom of each cell
- visible border lines between cells
- no title banner
- no extra decorations
- no extra animals
- no extra variants

Exact frame order:
Row 1: idle_00, idle_01, idle_02, idle_03, walk_00
Row 2: walk_01, walk_02, walk_03, walk_04, walk_05
Row 3: eat_00, eat_01, eat_02, eat_03, happy_00
Row 4: happy_01, happy_02, happy_03, sad_00, sad_01
Row 5: sleep_00, sleep_01, sick_00, sick_01, sick_02
Row 6: sick_03, bathe_00, bathe_01, bathe_02, bathe_03

Critical rules:
- exactly these 30 frames only
- do not skip frames
- do not duplicate frames
- do not invent extra walk frames
- do not rename any frame
- labels must exactly match the frame names above
- keep all cells evenly sized
- keep the sprite centered above the label in each cell
- keep the board as a rigid contact-sheet grid
- do not turn the board into a poster or model sheet

Style:
- pixel art
- crisp edges
- slightly moody, cute, vulnerable desktop-pet tone
- delicate legs, elegant face and ears, gentle posture, subtle antlers only if readable
- female should read slightly smaller and calmer
- blue should be the dominant palette family without losing deer readability

Animation intent:
- idle: subtle breathing or blinking
- walk: readable side-view walk cycle
- eat: nibble or drink motion
- happy: excited positive motion
- sad: slumped low-energy pose
- sleep: clearly asleep and distinct from idle
- sick: visibly unwell and distinct from sad
- bathe: self-cleaning or washing motion

Return only the generated image.
```

## 59. deer / female / indigo

```text
Generate an IMAGE ONLY.

Do not answer with text.
Do not describe limitations.
Do not provide configuration data.
Do not provide JSON.
Do not provide a table.
Do not provide a written explanation.

Create exactly one labeled sprite reference board image for this single variant only:

deer / female / indigo

This prompt is self-contained. Do not rely on previous images.

Board rules:
- one single board only
- exactly 5 columns and exactly 6 rows
- exactly 30 total cells
- every row must contain exactly 5 cells
- every column must contain exactly 6 cells
- all cells must be equal size
- no merged cells
- no missing cells
- no extra cells
- no irregular row widths
- one sprite per cell
- one frame label at the bottom of each cell
- visible border lines between cells
- no title banner
- no extra decorations
- no extra animals
- no extra variants

Exact frame order:
Row 1: idle_00, idle_01, idle_02, idle_03, walk_00
Row 2: walk_01, walk_02, walk_03, walk_04, walk_05
Row 3: eat_00, eat_01, eat_02, eat_03, happy_00
Row 4: happy_01, happy_02, happy_03, sad_00, sad_01
Row 5: sleep_00, sleep_01, sick_00, sick_01, sick_02
Row 6: sick_03, bathe_00, bathe_01, bathe_02, bathe_03

Critical rules:
- exactly these 30 frames only
- do not skip frames
- do not duplicate frames
- do not invent extra walk frames
- do not rename any frame
- labels must exactly match the frame names above
- keep all cells evenly sized
- keep the sprite centered above the label in each cell
- keep the board as a rigid contact-sheet grid
- do not turn the board into a poster or model sheet

Style:
- pixel art
- crisp edges
- slightly moody, cute, vulnerable desktop-pet tone
- delicate legs, elegant face and ears, gentle posture, subtle antlers only if readable
- female should read slightly smaller and calmer
- indigo should be the dominant palette family without losing deer readability

Animation intent:
- idle: subtle breathing or blinking
- walk: readable side-view walk cycle
- eat: nibble or drink motion
- happy: excited positive motion
- sad: slumped low-energy pose
- sleep: clearly asleep and distinct from idle
- sick: visibly unwell and distinct from sad
- bathe: self-cleaning or washing motion

Return only the generated image.
```

## 60. deer / female / violet

```text
Generate an IMAGE ONLY.

Do not answer with text.
Do not describe limitations.
Do not provide configuration data.
Do not provide JSON.
Do not provide a table.
Do not provide a written explanation.

Create exactly one labeled sprite reference board image for this single variant only:

deer / female / violet

This prompt is self-contained. Do not rely on previous images.

Board rules:
- one single board only
- exactly 5 columns and exactly 6 rows
- exactly 30 total cells
- every row must contain exactly 5 cells
- every column must contain exactly 6 cells
- all cells must be equal size
- no merged cells
- no missing cells
- no extra cells
- no irregular row widths
- one sprite per cell
- one frame label at the bottom of each cell
- visible border lines between cells
- no title banner
- no extra decorations
- no extra animals
- no extra variants

Exact frame order:
Row 1: idle_00, idle_01, idle_02, idle_03, walk_00
Row 2: walk_01, walk_02, walk_03, walk_04, walk_05
Row 3: eat_00, eat_01, eat_02, eat_03, happy_00
Row 4: happy_01, happy_02, happy_03, sad_00, sad_01
Row 5: sleep_00, sleep_01, sick_00, sick_01, sick_02
Row 6: sick_03, bathe_00, bathe_01, bathe_02, bathe_03

Critical rules:
- exactly these 30 frames only
- do not skip frames
- do not duplicate frames
- do not invent extra walk frames
- do not rename any frame
- labels must exactly match the frame names above
- keep all cells evenly sized
- keep the sprite centered above the label in each cell
- keep the board as a rigid contact-sheet grid
- do not turn the board into a poster or model sheet

Style:
- pixel art
- crisp edges
- slightly moody, cute, vulnerable desktop-pet tone
- delicate legs, elegant face and ears, gentle posture, subtle antlers only if readable
- female should read slightly smaller and calmer
- violet should be the dominant palette family without losing deer readability

Animation intent:
- idle: subtle breathing or blinking
- walk: readable side-view walk cycle
- eat: nibble or drink motion
- happy: excited positive motion
- sad: slumped low-energy pose
- sleep: clearly asleep and distinct from idle
- sick: visibly unwell and distinct from sad
- bathe: self-cleaning or washing motion

Return only the generated image.
```

## 61. frog / male / red

```text
Generate an IMAGE ONLY.

Do not answer with text.
Do not describe limitations.
Do not provide configuration data.
Do not provide JSON.
Do not provide a table.
Do not provide a written explanation.

Create exactly one labeled sprite reference board image for this single variant only:

frog / male / red

This prompt is self-contained. Do not rely on previous images.

Board rules:
- one single board only
- exactly 5 columns and exactly 6 rows
- exactly 30 total cells
- every row must contain exactly 5 cells
- every column must contain exactly 6 cells
- all cells must be equal size
- no merged cells
- no missing cells
- no extra cells
- no irregular row widths
- one sprite per cell
- one frame label at the bottom of each cell
- visible border lines between cells
- no title banner
- no extra decorations
- no extra animals
- no extra variants

Exact frame order:
Row 1: idle_00, idle_01, idle_02, idle_03, walk_00
Row 2: walk_01, walk_02, walk_03, walk_04, walk_05
Row 3: eat_00, eat_01, eat_02, eat_03, happy_00
Row 4: happy_01, happy_02, happy_03, sad_00, sad_01
Row 5: sleep_00, sleep_01, sick_00, sick_01, sick_02
Row 6: sick_03, bathe_00, bathe_01, bathe_02, bathe_03

Critical rules:
- exactly these 30 frames only
- do not skip frames
- do not duplicate frames
- do not invent extra walk frames
- do not rename any frame
- labels must exactly match the frame names above
- keep all cells evenly sized
- keep the sprite centered above the label in each cell
- keep the board as a rigid contact-sheet grid
- do not turn the board into a poster or model sheet

Style:
- pixel art
- crisp edges
- slightly moody, cute, vulnerable desktop-pet tone
- squat body, large eyes, springy silhouette
- male should read slightly larger and bolder
- red should be the dominant palette family without losing frog readability

Animation intent:
- idle: subtle breathing or blinking
- walk: readable side-view walk cycle
- eat: nibble or drink motion
- happy: excited positive motion
- sad: slumped low-energy pose
- sleep: clearly asleep and distinct from idle
- sick: visibly unwell and distinct from sad
- bathe: self-cleaning or washing motion

Return only the generated image.
```

## 62. frog / male / orange

```text
Generate an IMAGE ONLY.

Do not answer with text.
Do not describe limitations.
Do not provide configuration data.
Do not provide JSON.
Do not provide a table.
Do not provide a written explanation.

Create exactly one labeled sprite reference board image for this single variant only:

frog / male / orange

This prompt is self-contained. Do not rely on previous images.

Board rules:
- one single board only
- exactly 5 columns and exactly 6 rows
- exactly 30 total cells
- every row must contain exactly 5 cells
- every column must contain exactly 6 cells
- all cells must be equal size
- no merged cells
- no missing cells
- no extra cells
- no irregular row widths
- one sprite per cell
- one frame label at the bottom of each cell
- visible border lines between cells
- no title banner
- no extra decorations
- no extra animals
- no extra variants

Exact frame order:
Row 1: idle_00, idle_01, idle_02, idle_03, walk_00
Row 2: walk_01, walk_02, walk_03, walk_04, walk_05
Row 3: eat_00, eat_01, eat_02, eat_03, happy_00
Row 4: happy_01, happy_02, happy_03, sad_00, sad_01
Row 5: sleep_00, sleep_01, sick_00, sick_01, sick_02
Row 6: sick_03, bathe_00, bathe_01, bathe_02, bathe_03

Critical rules:
- exactly these 30 frames only
- do not skip frames
- do not duplicate frames
- do not invent extra walk frames
- do not rename any frame
- labels must exactly match the frame names above
- keep all cells evenly sized
- keep the sprite centered above the label in each cell
- keep the board as a rigid contact-sheet grid
- do not turn the board into a poster or model sheet

Style:
- pixel art
- crisp edges
- slightly moody, cute, vulnerable desktop-pet tone
- squat body, large eyes, springy silhouette
- male should read slightly larger and bolder
- orange should be the dominant palette family without losing frog readability

Animation intent:
- idle: subtle breathing or blinking
- walk: readable side-view walk cycle
- eat: nibble or drink motion
- happy: excited positive motion
- sad: slumped low-energy pose
- sleep: clearly asleep and distinct from idle
- sick: visibly unwell and distinct from sad
- bathe: self-cleaning or washing motion

Return only the generated image.
```

## 63. frog / male / yellow

```text
Generate an IMAGE ONLY.

Do not answer with text.
Do not describe limitations.
Do not provide configuration data.
Do not provide JSON.
Do not provide a table.
Do not provide a written explanation.

Create exactly one labeled sprite reference board image for this single variant only:

frog / male / yellow

This prompt is self-contained. Do not rely on previous images.

Board rules:
- one single board only
- exactly 5 columns and exactly 6 rows
- exactly 30 total cells
- every row must contain exactly 5 cells
- every column must contain exactly 6 cells
- all cells must be equal size
- no merged cells
- no missing cells
- no extra cells
- no irregular row widths
- one sprite per cell
- one frame label at the bottom of each cell
- visible border lines between cells
- no title banner
- no extra decorations
- no extra animals
- no extra variants

Exact frame order:
Row 1: idle_00, idle_01, idle_02, idle_03, walk_00
Row 2: walk_01, walk_02, walk_03, walk_04, walk_05
Row 3: eat_00, eat_01, eat_02, eat_03, happy_00
Row 4: happy_01, happy_02, happy_03, sad_00, sad_01
Row 5: sleep_00, sleep_01, sick_00, sick_01, sick_02
Row 6: sick_03, bathe_00, bathe_01, bathe_02, bathe_03

Critical rules:
- exactly these 30 frames only
- do not skip frames
- do not duplicate frames
- do not invent extra walk frames
- do not rename any frame
- labels must exactly match the frame names above
- keep all cells evenly sized
- keep the sprite centered above the label in each cell
- keep the board as a rigid contact-sheet grid
- do not turn the board into a poster or model sheet

Style:
- pixel art
- crisp edges
- slightly moody, cute, vulnerable desktop-pet tone
- squat body, large eyes, springy silhouette
- male should read slightly larger and bolder
- yellow should be the dominant palette family without losing frog readability

Animation intent:
- idle: subtle breathing or blinking
- walk: readable side-view walk cycle
- eat: nibble or drink motion
- happy: excited positive motion
- sad: slumped low-energy pose
- sleep: clearly asleep and distinct from idle
- sick: visibly unwell and distinct from sad
- bathe: self-cleaning or washing motion

Return only the generated image.
```

## 64. frog / male / blue

```text
Generate an IMAGE ONLY.

Do not answer with text.
Do not describe limitations.
Do not provide configuration data.
Do not provide JSON.
Do not provide a table.
Do not provide a written explanation.

Create exactly one labeled sprite reference board image for this single variant only:

frog / male / blue

This prompt is self-contained. Do not rely on previous images.

Board rules:
- one single board only
- exactly 5 columns and exactly 6 rows
- exactly 30 total cells
- every row must contain exactly 5 cells
- every column must contain exactly 6 cells
- all cells must be equal size
- no merged cells
- no missing cells
- no extra cells
- no irregular row widths
- one sprite per cell
- one frame label at the bottom of each cell
- visible border lines between cells
- no title banner
- no extra decorations
- no extra animals
- no extra variants

Exact frame order:
Row 1: idle_00, idle_01, idle_02, idle_03, walk_00
Row 2: walk_01, walk_02, walk_03, walk_04, walk_05
Row 3: eat_00, eat_01, eat_02, eat_03, happy_00
Row 4: happy_01, happy_02, happy_03, sad_00, sad_01
Row 5: sleep_00, sleep_01, sick_00, sick_01, sick_02
Row 6: sick_03, bathe_00, bathe_01, bathe_02, bathe_03

Critical rules:
- exactly these 30 frames only
- do not skip frames
- do not duplicate frames
- do not invent extra walk frames
- do not rename any frame
- labels must exactly match the frame names above
- keep all cells evenly sized
- keep the sprite centered above the label in each cell
- keep the board as a rigid contact-sheet grid
- do not turn the board into a poster or model sheet

Style:
- pixel art
- crisp edges
- slightly moody, cute, vulnerable desktop-pet tone
- squat body, large eyes, springy silhouette
- male should read slightly larger and bolder
- blue should be the dominant palette family without losing frog readability

Animation intent:
- idle: subtle breathing or blinking
- walk: readable side-view walk cycle
- eat: nibble or drink motion
- happy: excited positive motion
- sad: slumped low-energy pose
- sleep: clearly asleep and distinct from idle
- sick: visibly unwell and distinct from sad
- bathe: self-cleaning or washing motion

Return only the generated image.
```

## 65. frog / male / indigo

```text
Generate an IMAGE ONLY.

Do not answer with text.
Do not describe limitations.
Do not provide configuration data.
Do not provide JSON.
Do not provide a table.
Do not provide a written explanation.

Create exactly one labeled sprite reference board image for this single variant only:

frog / male / indigo

This prompt is self-contained. Do not rely on previous images.

Board rules:
- one single board only
- exactly 5 columns and exactly 6 rows
- exactly 30 total cells
- every row must contain exactly 5 cells
- every column must contain exactly 6 cells
- all cells must be equal size
- no merged cells
- no missing cells
- no extra cells
- no irregular row widths
- one sprite per cell
- one frame label at the bottom of each cell
- visible border lines between cells
- no title banner
- no extra decorations
- no extra animals
- no extra variants

Exact frame order:
Row 1: idle_00, idle_01, idle_02, idle_03, walk_00
Row 2: walk_01, walk_02, walk_03, walk_04, walk_05
Row 3: eat_00, eat_01, eat_02, eat_03, happy_00
Row 4: happy_01, happy_02, happy_03, sad_00, sad_01
Row 5: sleep_00, sleep_01, sick_00, sick_01, sick_02
Row 6: sick_03, bathe_00, bathe_01, bathe_02, bathe_03

Critical rules:
- exactly these 30 frames only
- do not skip frames
- do not duplicate frames
- do not invent extra walk frames
- do not rename any frame
- labels must exactly match the frame names above
- keep all cells evenly sized
- keep the sprite centered above the label in each cell
- keep the board as a rigid contact-sheet grid
- do not turn the board into a poster or model sheet

Style:
- pixel art
- crisp edges
- slightly moody, cute, vulnerable desktop-pet tone
- squat body, large eyes, springy silhouette
- male should read slightly larger and bolder
- indigo should be the dominant palette family without losing frog readability

Animation intent:
- idle: subtle breathing or blinking
- walk: readable side-view walk cycle
- eat: nibble or drink motion
- happy: excited positive motion
- sad: slumped low-energy pose
- sleep: clearly asleep and distinct from idle
- sick: visibly unwell and distinct from sad
- bathe: self-cleaning or washing motion

Return only the generated image.
```

## 66. frog / male / violet

```text
Generate an IMAGE ONLY.

Do not answer with text.
Do not describe limitations.
Do not provide configuration data.
Do not provide JSON.
Do not provide a table.
Do not provide a written explanation.

Create exactly one labeled sprite reference board image for this single variant only:

frog / male / violet

This prompt is self-contained. Do not rely on previous images.

Board rules:
- one single board only
- exactly 5 columns and exactly 6 rows
- exactly 30 total cells
- every row must contain exactly 5 cells
- every column must contain exactly 6 cells
- all cells must be equal size
- no merged cells
- no missing cells
- no extra cells
- no irregular row widths
- one sprite per cell
- one frame label at the bottom of each cell
- visible border lines between cells
- no title banner
- no extra decorations
- no extra animals
- no extra variants

Exact frame order:
Row 1: idle_00, idle_01, idle_02, idle_03, walk_00
Row 2: walk_01, walk_02, walk_03, walk_04, walk_05
Row 3: eat_00, eat_01, eat_02, eat_03, happy_00
Row 4: happy_01, happy_02, happy_03, sad_00, sad_01
Row 5: sleep_00, sleep_01, sick_00, sick_01, sick_02
Row 6: sick_03, bathe_00, bathe_01, bathe_02, bathe_03

Critical rules:
- exactly these 30 frames only
- do not skip frames
- do not duplicate frames
- do not invent extra walk frames
- do not rename any frame
- labels must exactly match the frame names above
- keep all cells evenly sized
- keep the sprite centered above the label in each cell
- keep the board as a rigid contact-sheet grid
- do not turn the board into a poster or model sheet

Style:
- pixel art
- crisp edges
- slightly moody, cute, vulnerable desktop-pet tone
- squat body, large eyes, springy silhouette
- male should read slightly larger and bolder
- violet should be the dominant palette family without losing frog readability

Animation intent:
- idle: subtle breathing or blinking
- walk: readable side-view walk cycle
- eat: nibble or drink motion
- happy: excited positive motion
- sad: slumped low-energy pose
- sleep: clearly asleep and distinct from idle
- sick: visibly unwell and distinct from sad
- bathe: self-cleaning or washing motion

Return only the generated image.
```

## 67. frog / female / red

```text
Generate an IMAGE ONLY.

Do not answer with text.
Do not describe limitations.
Do not provide configuration data.
Do not provide JSON.
Do not provide a table.
Do not provide a written explanation.

Create exactly one labeled sprite reference board image for this single variant only:

frog / female / red

This prompt is self-contained. Do not rely on previous images.

Board rules:
- one single board only
- exactly 5 columns and exactly 6 rows
- exactly 30 total cells
- every row must contain exactly 5 cells
- every column must contain exactly 6 cells
- all cells must be equal size
- no merged cells
- no missing cells
- no extra cells
- no irregular row widths
- one sprite per cell
- one frame label at the bottom of each cell
- visible border lines between cells
- no title banner
- no extra decorations
- no extra animals
- no extra variants

Exact frame order:
Row 1: idle_00, idle_01, idle_02, idle_03, walk_00
Row 2: walk_01, walk_02, walk_03, walk_04, walk_05
Row 3: eat_00, eat_01, eat_02, eat_03, happy_00
Row 4: happy_01, happy_02, happy_03, sad_00, sad_01
Row 5: sleep_00, sleep_01, sick_00, sick_01, sick_02
Row 6: sick_03, bathe_00, bathe_01, bathe_02, bathe_03

Critical rules:
- exactly these 30 frames only
- do not skip frames
- do not duplicate frames
- do not invent extra walk frames
- do not rename any frame
- labels must exactly match the frame names above
- keep all cells evenly sized
- keep the sprite centered above the label in each cell
- keep the board as a rigid contact-sheet grid
- do not turn the board into a poster or model sheet

Style:
- pixel art
- crisp edges
- slightly moody, cute, vulnerable desktop-pet tone
- squat body, large eyes, springy silhouette
- female should read slightly smaller and calmer
- red should be the dominant palette family without losing frog readability

Animation intent:
- idle: subtle breathing or blinking
- walk: readable side-view walk cycle
- eat: nibble or drink motion
- happy: excited positive motion
- sad: slumped low-energy pose
- sleep: clearly asleep and distinct from idle
- sick: visibly unwell and distinct from sad
- bathe: self-cleaning or washing motion

Return only the generated image.
```

## 68. frog / female / orange

```text
Generate an IMAGE ONLY.

Do not answer with text.
Do not describe limitations.
Do not provide configuration data.
Do not provide JSON.
Do not provide a table.
Do not provide a written explanation.

Create exactly one labeled sprite reference board image for this single variant only:

frog / female / orange

This prompt is self-contained. Do not rely on previous images.

Board rules:
- one single board only
- exactly 5 columns and exactly 6 rows
- exactly 30 total cells
- every row must contain exactly 5 cells
- every column must contain exactly 6 cells
- all cells must be equal size
- no merged cells
- no missing cells
- no extra cells
- no irregular row widths
- one sprite per cell
- one frame label at the bottom of each cell
- visible border lines between cells
- no title banner
- no extra decorations
- no extra animals
- no extra variants

Exact frame order:
Row 1: idle_00, idle_01, idle_02, idle_03, walk_00
Row 2: walk_01, walk_02, walk_03, walk_04, walk_05
Row 3: eat_00, eat_01, eat_02, eat_03, happy_00
Row 4: happy_01, happy_02, happy_03, sad_00, sad_01
Row 5: sleep_00, sleep_01, sick_00, sick_01, sick_02
Row 6: sick_03, bathe_00, bathe_01, bathe_02, bathe_03

Critical rules:
- exactly these 30 frames only
- do not skip frames
- do not duplicate frames
- do not invent extra walk frames
- do not rename any frame
- labels must exactly match the frame names above
- keep all cells evenly sized
- keep the sprite centered above the label in each cell
- keep the board as a rigid contact-sheet grid
- do not turn the board into a poster or model sheet

Style:
- pixel art
- crisp edges
- slightly moody, cute, vulnerable desktop-pet tone
- squat body, large eyes, springy silhouette
- female should read slightly smaller and calmer
- orange should be the dominant palette family without losing frog readability

Animation intent:
- idle: subtle breathing or blinking
- walk: readable side-view walk cycle
- eat: nibble or drink motion
- happy: excited positive motion
- sad: slumped low-energy pose
- sleep: clearly asleep and distinct from idle
- sick: visibly unwell and distinct from sad
- bathe: self-cleaning or washing motion

Return only the generated image.
```

## 69. frog / female / yellow

```text
Generate an IMAGE ONLY.

Do not answer with text.
Do not describe limitations.
Do not provide configuration data.
Do not provide JSON.
Do not provide a table.
Do not provide a written explanation.

Create exactly one labeled sprite reference board image for this single variant only:

frog / female / yellow

This prompt is self-contained. Do not rely on previous images.

Board rules:
- one single board only
- exactly 5 columns and exactly 6 rows
- exactly 30 total cells
- every row must contain exactly 5 cells
- every column must contain exactly 6 cells
- all cells must be equal size
- no merged cells
- no missing cells
- no extra cells
- no irregular row widths
- one sprite per cell
- one frame label at the bottom of each cell
- visible border lines between cells
- no title banner
- no extra decorations
- no extra animals
- no extra variants

Exact frame order:
Row 1: idle_00, idle_01, idle_02, idle_03, walk_00
Row 2: walk_01, walk_02, walk_03, walk_04, walk_05
Row 3: eat_00, eat_01, eat_02, eat_03, happy_00
Row 4: happy_01, happy_02, happy_03, sad_00, sad_01
Row 5: sleep_00, sleep_01, sick_00, sick_01, sick_02
Row 6: sick_03, bathe_00, bathe_01, bathe_02, bathe_03

Critical rules:
- exactly these 30 frames only
- do not skip frames
- do not duplicate frames
- do not invent extra walk frames
- do not rename any frame
- labels must exactly match the frame names above
- keep all cells evenly sized
- keep the sprite centered above the label in each cell
- keep the board as a rigid contact-sheet grid
- do not turn the board into a poster or model sheet

Style:
- pixel art
- crisp edges
- slightly moody, cute, vulnerable desktop-pet tone
- squat body, large eyes, springy silhouette
- female should read slightly smaller and calmer
- yellow should be the dominant palette family without losing frog readability

Animation intent:
- idle: subtle breathing or blinking
- walk: readable side-view walk cycle
- eat: nibble or drink motion
- happy: excited positive motion
- sad: slumped low-energy pose
- sleep: clearly asleep and distinct from idle
- sick: visibly unwell and distinct from sad
- bathe: self-cleaning or washing motion

Return only the generated image.
```

## 70. frog / female / blue

```text
Generate an IMAGE ONLY.

Do not answer with text.
Do not describe limitations.
Do not provide configuration data.
Do not provide JSON.
Do not provide a table.
Do not provide a written explanation.

Create exactly one labeled sprite reference board image for this single variant only:

frog / female / blue

This prompt is self-contained. Do not rely on previous images.

Board rules:
- one single board only
- exactly 5 columns and exactly 6 rows
- exactly 30 total cells
- every row must contain exactly 5 cells
- every column must contain exactly 6 cells
- all cells must be equal size
- no merged cells
- no missing cells
- no extra cells
- no irregular row widths
- one sprite per cell
- one frame label at the bottom of each cell
- visible border lines between cells
- no title banner
- no extra decorations
- no extra animals
- no extra variants

Exact frame order:
Row 1: idle_00, idle_01, idle_02, idle_03, walk_00
Row 2: walk_01, walk_02, walk_03, walk_04, walk_05
Row 3: eat_00, eat_01, eat_02, eat_03, happy_00
Row 4: happy_01, happy_02, happy_03, sad_00, sad_01
Row 5: sleep_00, sleep_01, sick_00, sick_01, sick_02
Row 6: sick_03, bathe_00, bathe_01, bathe_02, bathe_03

Critical rules:
- exactly these 30 frames only
- do not skip frames
- do not duplicate frames
- do not invent extra walk frames
- do not rename any frame
- labels must exactly match the frame names above
- keep all cells evenly sized
- keep the sprite centered above the label in each cell
- keep the board as a rigid contact-sheet grid
- do not turn the board into a poster or model sheet

Style:
- pixel art
- crisp edges
- slightly moody, cute, vulnerable desktop-pet tone
- squat body, large eyes, springy silhouette
- female should read slightly smaller and calmer
- blue should be the dominant palette family without losing frog readability

Animation intent:
- idle: subtle breathing or blinking
- walk: readable side-view walk cycle
- eat: nibble or drink motion
- happy: excited positive motion
- sad: slumped low-energy pose
- sleep: clearly asleep and distinct from idle
- sick: visibly unwell and distinct from sad
- bathe: self-cleaning or washing motion

Return only the generated image.
```

## 71. frog / female / indigo

```text
Generate an IMAGE ONLY.

Do not answer with text.
Do not describe limitations.
Do not provide configuration data.
Do not provide JSON.
Do not provide a table.
Do not provide a written explanation.

Create exactly one labeled sprite reference board image for this single variant only:

frog / female / indigo

This prompt is self-contained. Do not rely on previous images.

Board rules:
- one single board only
- exactly 5 columns and exactly 6 rows
- exactly 30 total cells
- every row must contain exactly 5 cells
- every column must contain exactly 6 cells
- all cells must be equal size
- no merged cells
- no missing cells
- no extra cells
- no irregular row widths
- one sprite per cell
- one frame label at the bottom of each cell
- visible border lines between cells
- no title banner
- no extra decorations
- no extra animals
- no extra variants

Exact frame order:
Row 1: idle_00, idle_01, idle_02, idle_03, walk_00
Row 2: walk_01, walk_02, walk_03, walk_04, walk_05
Row 3: eat_00, eat_01, eat_02, eat_03, happy_00
Row 4: happy_01, happy_02, happy_03, sad_00, sad_01
Row 5: sleep_00, sleep_01, sick_00, sick_01, sick_02
Row 6: sick_03, bathe_00, bathe_01, bathe_02, bathe_03

Critical rules:
- exactly these 30 frames only
- do not skip frames
- do not duplicate frames
- do not invent extra walk frames
- do not rename any frame
- labels must exactly match the frame names above
- keep all cells evenly sized
- keep the sprite centered above the label in each cell
- keep the board as a rigid contact-sheet grid
- do not turn the board into a poster or model sheet

Style:
- pixel art
- crisp edges
- slightly moody, cute, vulnerable desktop-pet tone
- squat body, large eyes, springy silhouette
- female should read slightly smaller and calmer
- indigo should be the dominant palette family without losing frog readability

Animation intent:
- idle: subtle breathing or blinking
- walk: readable side-view walk cycle
- eat: nibble or drink motion
- happy: excited positive motion
- sad: slumped low-energy pose
- sleep: clearly asleep and distinct from idle
- sick: visibly unwell and distinct from sad
- bathe: self-cleaning or washing motion

Return only the generated image.
```

## 72. frog / female / violet

```text
Generate an IMAGE ONLY.

Do not answer with text.
Do not describe limitations.
Do not provide configuration data.
Do not provide JSON.
Do not provide a table.
Do not provide a written explanation.

Create exactly one labeled sprite reference board image for this single variant only:

frog / female / violet

This prompt is self-contained. Do not rely on previous images.

Board rules:
- one single board only
- exactly 5 columns and exactly 6 rows
- exactly 30 total cells
- every row must contain exactly 5 cells
- every column must contain exactly 6 cells
- all cells must be equal size
- no merged cells
- no missing cells
- no extra cells
- no irregular row widths
- one sprite per cell
- one frame label at the bottom of each cell
- visible border lines between cells
- no title banner
- no extra decorations
- no extra animals
- no extra variants

Exact frame order:
Row 1: idle_00, idle_01, idle_02, idle_03, walk_00
Row 2: walk_01, walk_02, walk_03, walk_04, walk_05
Row 3: eat_00, eat_01, eat_02, eat_03, happy_00
Row 4: happy_01, happy_02, happy_03, sad_00, sad_01
Row 5: sleep_00, sleep_01, sick_00, sick_01, sick_02
Row 6: sick_03, bathe_00, bathe_01, bathe_02, bathe_03

Critical rules:
- exactly these 30 frames only
- do not skip frames
- do not duplicate frames
- do not invent extra walk frames
- do not rename any frame
- labels must exactly match the frame names above
- keep all cells evenly sized
- keep the sprite centered above the label in each cell
- keep the board as a rigid contact-sheet grid
- do not turn the board into a poster or model sheet

Style:
- pixel art
- crisp edges
- slightly moody, cute, vulnerable desktop-pet tone
- squat body, large eyes, springy silhouette
- female should read slightly smaller and calmer
- violet should be the dominant palette family without losing frog readability

Animation intent:
- idle: subtle breathing or blinking
- walk: readable side-view walk cycle
- eat: nibble or drink motion
- happy: excited positive motion
- sad: slumped low-energy pose
- sleep: clearly asleep and distinct from idle
- sick: visibly unwell and distinct from sad
- bathe: self-cleaning or washing motion

Return only the generated image.
```

## 73. pigeon / male / red

```text
Generate an IMAGE ONLY.

Do not answer with text.
Do not describe limitations.
Do not provide configuration data.
Do not provide JSON.
Do not provide a table.
Do not provide a written explanation.

Create exactly one labeled sprite reference board image for this single variant only:

pigeon / male / red

This prompt is self-contained. Do not rely on previous images.

Board rules:
- one single board only
- exactly 5 columns and exactly 6 rows
- exactly 30 total cells
- every row must contain exactly 5 cells
- every column must contain exactly 6 cells
- all cells must be equal size
- no merged cells
- no missing cells
- no extra cells
- no irregular row widths
- one sprite per cell
- one frame label at the bottom of each cell
- visible border lines between cells
- no title banner
- no extra decorations
- no extra animals
- no extra variants

Exact frame order:
Row 1: idle_00, idle_01, idle_02, idle_03, walk_00
Row 2: walk_01, walk_02, walk_03, walk_04, walk_05
Row 3: eat_00, eat_01, eat_02, eat_03, happy_00
Row 4: happy_01, happy_02, happy_03, sad_00, sad_01
Row 5: sleep_00, sleep_01, sick_00, sick_01, sick_02
Row 6: sick_03, bathe_00, bathe_01, bathe_02, bathe_03

Critical rules:
- exactly these 30 frames only
- do not skip frames
- do not duplicate frames
- do not invent extra walk frames
- do not rename any frame
- labels must exactly match the frame names above
- keep all cells evenly sized
- keep the sprite centered above the label in each cell
- keep the board as a rigid contact-sheet grid
- do not turn the board into a poster or model sheet

Style:
- pixel art
- crisp edges
- slightly moody, cute, vulnerable desktop-pet tone
- rounder than crow, puffed chest, shorter beak, awkward but lovable posture
- male should read slightly larger and bolder
- red should be the dominant palette family without losing pigeon readability

Animation intent:
- idle: subtle breathing or blinking
- walk: readable side-view walk cycle
- eat: nibble or drink motion
- happy: excited positive motion
- sad: slumped low-energy pose
- sleep: clearly asleep and distinct from idle
- sick: visibly unwell and distinct from sad
- bathe: self-cleaning or washing motion

Return only the generated image.
```

## 74. pigeon / male / orange

```text
Generate an IMAGE ONLY.

Do not answer with text.
Do not describe limitations.
Do not provide configuration data.
Do not provide JSON.
Do not provide a table.
Do not provide a written explanation.

Create exactly one labeled sprite reference board image for this single variant only:

pigeon / male / orange

This prompt is self-contained. Do not rely on previous images.

Board rules:
- one single board only
- exactly 5 columns and exactly 6 rows
- exactly 30 total cells
- every row must contain exactly 5 cells
- every column must contain exactly 6 cells
- all cells must be equal size
- no merged cells
- no missing cells
- no extra cells
- no irregular row widths
- one sprite per cell
- one frame label at the bottom of each cell
- visible border lines between cells
- no title banner
- no extra decorations
- no extra animals
- no extra variants

Exact frame order:
Row 1: idle_00, idle_01, idle_02, idle_03, walk_00
Row 2: walk_01, walk_02, walk_03, walk_04, walk_05
Row 3: eat_00, eat_01, eat_02, eat_03, happy_00
Row 4: happy_01, happy_02, happy_03, sad_00, sad_01
Row 5: sleep_00, sleep_01, sick_00, sick_01, sick_02
Row 6: sick_03, bathe_00, bathe_01, bathe_02, bathe_03

Critical rules:
- exactly these 30 frames only
- do not skip frames
- do not duplicate frames
- do not invent extra walk frames
- do not rename any frame
- labels must exactly match the frame names above
- keep all cells evenly sized
- keep the sprite centered above the label in each cell
- keep the board as a rigid contact-sheet grid
- do not turn the board into a poster or model sheet

Style:
- pixel art
- crisp edges
- slightly moody, cute, vulnerable desktop-pet tone
- rounder than crow, puffed chest, shorter beak, awkward but lovable posture
- male should read slightly larger and bolder
- orange should be the dominant palette family without losing pigeon readability

Animation intent:
- idle: subtle breathing or blinking
- walk: readable side-view walk cycle
- eat: nibble or drink motion
- happy: excited positive motion
- sad: slumped low-energy pose
- sleep: clearly asleep and distinct from idle
- sick: visibly unwell and distinct from sad
- bathe: self-cleaning or washing motion

Return only the generated image.
```

## 75. pigeon / male / yellow

```text
Generate an IMAGE ONLY.

Do not answer with text.
Do not describe limitations.
Do not provide configuration data.
Do not provide JSON.
Do not provide a table.
Do not provide a written explanation.

Create exactly one labeled sprite reference board image for this single variant only:

pigeon / male / yellow

This prompt is self-contained. Do not rely on previous images.

Board rules:
- one single board only
- exactly 5 columns and exactly 6 rows
- exactly 30 total cells
- every row must contain exactly 5 cells
- every column must contain exactly 6 cells
- all cells must be equal size
- no merged cells
- no missing cells
- no extra cells
- no irregular row widths
- one sprite per cell
- one frame label at the bottom of each cell
- visible border lines between cells
- no title banner
- no extra decorations
- no extra animals
- no extra variants

Exact frame order:
Row 1: idle_00, idle_01, idle_02, idle_03, walk_00
Row 2: walk_01, walk_02, walk_03, walk_04, walk_05
Row 3: eat_00, eat_01, eat_02, eat_03, happy_00
Row 4: happy_01, happy_02, happy_03, sad_00, sad_01
Row 5: sleep_00, sleep_01, sick_00, sick_01, sick_02
Row 6: sick_03, bathe_00, bathe_01, bathe_02, bathe_03

Critical rules:
- exactly these 30 frames only
- do not skip frames
- do not duplicate frames
- do not invent extra walk frames
- do not rename any frame
- labels must exactly match the frame names above
- keep all cells evenly sized
- keep the sprite centered above the label in each cell
- keep the board as a rigid contact-sheet grid
- do not turn the board into a poster or model sheet

Style:
- pixel art
- crisp edges
- slightly moody, cute, vulnerable desktop-pet tone
- rounder than crow, puffed chest, shorter beak, awkward but lovable posture
- male should read slightly larger and bolder
- yellow should be the dominant palette family without losing pigeon readability

Animation intent:
- idle: subtle breathing or blinking
- walk: readable side-view walk cycle
- eat: nibble or drink motion
- happy: excited positive motion
- sad: slumped low-energy pose
- sleep: clearly asleep and distinct from idle
- sick: visibly unwell and distinct from sad
- bathe: self-cleaning or washing motion

Return only the generated image.
```

## 76. pigeon / male / blue

```text
Generate an IMAGE ONLY.

Do not answer with text.
Do not describe limitations.
Do not provide configuration data.
Do not provide JSON.
Do not provide a table.
Do not provide a written explanation.

Create exactly one labeled sprite reference board image for this single variant only:

pigeon / male / blue

This prompt is self-contained. Do not rely on previous images.

Board rules:
- one single board only
- exactly 5 columns and exactly 6 rows
- exactly 30 total cells
- every row must contain exactly 5 cells
- every column must contain exactly 6 cells
- all cells must be equal size
- no merged cells
- no missing cells
- no extra cells
- no irregular row widths
- one sprite per cell
- one frame label at the bottom of each cell
- visible border lines between cells
- no title banner
- no extra decorations
- no extra animals
- no extra variants

Exact frame order:
Row 1: idle_00, idle_01, idle_02, idle_03, walk_00
Row 2: walk_01, walk_02, walk_03, walk_04, walk_05
Row 3: eat_00, eat_01, eat_02, eat_03, happy_00
Row 4: happy_01, happy_02, happy_03, sad_00, sad_01
Row 5: sleep_00, sleep_01, sick_00, sick_01, sick_02
Row 6: sick_03, bathe_00, bathe_01, bathe_02, bathe_03

Critical rules:
- exactly these 30 frames only
- do not skip frames
- do not duplicate frames
- do not invent extra walk frames
- do not rename any frame
- labels must exactly match the frame names above
- keep all cells evenly sized
- keep the sprite centered above the label in each cell
- keep the board as a rigid contact-sheet grid
- do not turn the board into a poster or model sheet

Style:
- pixel art
- crisp edges
- slightly moody, cute, vulnerable desktop-pet tone
- rounder than crow, puffed chest, shorter beak, awkward but lovable posture
- male should read slightly larger and bolder
- blue should be the dominant palette family without losing pigeon readability

Animation intent:
- idle: subtle breathing or blinking
- walk: readable side-view walk cycle
- eat: nibble or drink motion
- happy: excited positive motion
- sad: slumped low-energy pose
- sleep: clearly asleep and distinct from idle
- sick: visibly unwell and distinct from sad
- bathe: self-cleaning or washing motion

Return only the generated image.
```

## 77. pigeon / male / indigo

```text
Generate an IMAGE ONLY.

Do not answer with text.
Do not describe limitations.
Do not provide configuration data.
Do not provide JSON.
Do not provide a table.
Do not provide a written explanation.

Create exactly one labeled sprite reference board image for this single variant only:

pigeon / male / indigo

This prompt is self-contained. Do not rely on previous images.

Board rules:
- one single board only
- exactly 5 columns and exactly 6 rows
- exactly 30 total cells
- every row must contain exactly 5 cells
- every column must contain exactly 6 cells
- all cells must be equal size
- no merged cells
- no missing cells
- no extra cells
- no irregular row widths
- one sprite per cell
- one frame label at the bottom of each cell
- visible border lines between cells
- no title banner
- no extra decorations
- no extra animals
- no extra variants

Exact frame order:
Row 1: idle_00, idle_01, idle_02, idle_03, walk_00
Row 2: walk_01, walk_02, walk_03, walk_04, walk_05
Row 3: eat_00, eat_01, eat_02, eat_03, happy_00
Row 4: happy_01, happy_02, happy_03, sad_00, sad_01
Row 5: sleep_00, sleep_01, sick_00, sick_01, sick_02
Row 6: sick_03, bathe_00, bathe_01, bathe_02, bathe_03

Critical rules:
- exactly these 30 frames only
- do not skip frames
- do not duplicate frames
- do not invent extra walk frames
- do not rename any frame
- labels must exactly match the frame names above
- keep all cells evenly sized
- keep the sprite centered above the label in each cell
- keep the board as a rigid contact-sheet grid
- do not turn the board into a poster or model sheet

Style:
- pixel art
- crisp edges
- slightly moody, cute, vulnerable desktop-pet tone
- rounder than crow, puffed chest, shorter beak, awkward but lovable posture
- male should read slightly larger and bolder
- indigo should be the dominant palette family without losing pigeon readability

Animation intent:
- idle: subtle breathing or blinking
- walk: readable side-view walk cycle
- eat: nibble or drink motion
- happy: excited positive motion
- sad: slumped low-energy pose
- sleep: clearly asleep and distinct from idle
- sick: visibly unwell and distinct from sad
- bathe: self-cleaning or washing motion

Return only the generated image.
```

## 78. pigeon / male / violet

```text
Generate an IMAGE ONLY.

Do not answer with text.
Do not describe limitations.
Do not provide configuration data.
Do not provide JSON.
Do not provide a table.
Do not provide a written explanation.

Create exactly one labeled sprite reference board image for this single variant only:

pigeon / male / violet

This prompt is self-contained. Do not rely on previous images.

Board rules:
- one single board only
- exactly 5 columns and exactly 6 rows
- exactly 30 total cells
- every row must contain exactly 5 cells
- every column must contain exactly 6 cells
- all cells must be equal size
- no merged cells
- no missing cells
- no extra cells
- no irregular row widths
- one sprite per cell
- one frame label at the bottom of each cell
- visible border lines between cells
- no title banner
- no extra decorations
- no extra animals
- no extra variants

Exact frame order:
Row 1: idle_00, idle_01, idle_02, idle_03, walk_00
Row 2: walk_01, walk_02, walk_03, walk_04, walk_05
Row 3: eat_00, eat_01, eat_02, eat_03, happy_00
Row 4: happy_01, happy_02, happy_03, sad_00, sad_01
Row 5: sleep_00, sleep_01, sick_00, sick_01, sick_02
Row 6: sick_03, bathe_00, bathe_01, bathe_02, bathe_03

Critical rules:
- exactly these 30 frames only
- do not skip frames
- do not duplicate frames
- do not invent extra walk frames
- do not rename any frame
- labels must exactly match the frame names above
- keep all cells evenly sized
- keep the sprite centered above the label in each cell
- keep the board as a rigid contact-sheet grid
- do not turn the board into a poster or model sheet

Style:
- pixel art
- crisp edges
- slightly moody, cute, vulnerable desktop-pet tone
- rounder than crow, puffed chest, shorter beak, awkward but lovable posture
- male should read slightly larger and bolder
- violet should be the dominant palette family without losing pigeon readability

Animation intent:
- idle: subtle breathing or blinking
- walk: readable side-view walk cycle
- eat: nibble or drink motion
- happy: excited positive motion
- sad: slumped low-energy pose
- sleep: clearly asleep and distinct from idle
- sick: visibly unwell and distinct from sad
- bathe: self-cleaning or washing motion

Return only the generated image.
```

## 79. pigeon / female / red

```text
Generate an IMAGE ONLY.

Do not answer with text.
Do not describe limitations.
Do not provide configuration data.
Do not provide JSON.
Do not provide a table.
Do not provide a written explanation.

Create exactly one labeled sprite reference board image for this single variant only:

pigeon / female / red

This prompt is self-contained. Do not rely on previous images.

Board rules:
- one single board only
- exactly 5 columns and exactly 6 rows
- exactly 30 total cells
- every row must contain exactly 5 cells
- every column must contain exactly 6 cells
- all cells must be equal size
- no merged cells
- no missing cells
- no extra cells
- no irregular row widths
- one sprite per cell
- one frame label at the bottom of each cell
- visible border lines between cells
- no title banner
- no extra decorations
- no extra animals
- no extra variants

Exact frame order:
Row 1: idle_00, idle_01, idle_02, idle_03, walk_00
Row 2: walk_01, walk_02, walk_03, walk_04, walk_05
Row 3: eat_00, eat_01, eat_02, eat_03, happy_00
Row 4: happy_01, happy_02, happy_03, sad_00, sad_01
Row 5: sleep_00, sleep_01, sick_00, sick_01, sick_02
Row 6: sick_03, bathe_00, bathe_01, bathe_02, bathe_03

Critical rules:
- exactly these 30 frames only
- do not skip frames
- do not duplicate frames
- do not invent extra walk frames
- do not rename any frame
- labels must exactly match the frame names above
- keep all cells evenly sized
- keep the sprite centered above the label in each cell
- keep the board as a rigid contact-sheet grid
- do not turn the board into a poster or model sheet

Style:
- pixel art
- crisp edges
- slightly moody, cute, vulnerable desktop-pet tone
- rounder than crow, puffed chest, shorter beak, awkward but lovable posture
- female should read slightly smaller and calmer
- red should be the dominant palette family without losing pigeon readability

Animation intent:
- idle: subtle breathing or blinking
- walk: readable side-view walk cycle
- eat: nibble or drink motion
- happy: excited positive motion
- sad: slumped low-energy pose
- sleep: clearly asleep and distinct from idle
- sick: visibly unwell and distinct from sad
- bathe: self-cleaning or washing motion

Return only the generated image.
```

## 80. pigeon / female / orange

```text
Generate an IMAGE ONLY.

Do not answer with text.
Do not describe limitations.
Do not provide configuration data.
Do not provide JSON.
Do not provide a table.
Do not provide a written explanation.

Create exactly one labeled sprite reference board image for this single variant only:

pigeon / female / orange

This prompt is self-contained. Do not rely on previous images.

Board rules:
- one single board only
- exactly 5 columns and exactly 6 rows
- exactly 30 total cells
- every row must contain exactly 5 cells
- every column must contain exactly 6 cells
- all cells must be equal size
- no merged cells
- no missing cells
- no extra cells
- no irregular row widths
- one sprite per cell
- one frame label at the bottom of each cell
- visible border lines between cells
- no title banner
- no extra decorations
- no extra animals
- no extra variants

Exact frame order:
Row 1: idle_00, idle_01, idle_02, idle_03, walk_00
Row 2: walk_01, walk_02, walk_03, walk_04, walk_05
Row 3: eat_00, eat_01, eat_02, eat_03, happy_00
Row 4: happy_01, happy_02, happy_03, sad_00, sad_01
Row 5: sleep_00, sleep_01, sick_00, sick_01, sick_02
Row 6: sick_03, bathe_00, bathe_01, bathe_02, bathe_03

Critical rules:
- exactly these 30 frames only
- do not skip frames
- do not duplicate frames
- do not invent extra walk frames
- do not rename any frame
- labels must exactly match the frame names above
- keep all cells evenly sized
- keep the sprite centered above the label in each cell
- keep the board as a rigid contact-sheet grid
- do not turn the board into a poster or model sheet

Style:
- pixel art
- crisp edges
- slightly moody, cute, vulnerable desktop-pet tone
- rounder than crow, puffed chest, shorter beak, awkward but lovable posture
- female should read slightly smaller and calmer
- orange should be the dominant palette family without losing pigeon readability

Animation intent:
- idle: subtle breathing or blinking
- walk: readable side-view walk cycle
- eat: nibble or drink motion
- happy: excited positive motion
- sad: slumped low-energy pose
- sleep: clearly asleep and distinct from idle
- sick: visibly unwell and distinct from sad
- bathe: self-cleaning or washing motion

Return only the generated image.
```

## 81. pigeon / female / yellow

```text
Generate an IMAGE ONLY.

Do not answer with text.
Do not describe limitations.
Do not provide configuration data.
Do not provide JSON.
Do not provide a table.
Do not provide a written explanation.

Create exactly one labeled sprite reference board image for this single variant only:

pigeon / female / yellow

This prompt is self-contained. Do not rely on previous images.

Board rules:
- one single board only
- exactly 5 columns and exactly 6 rows
- exactly 30 total cells
- every row must contain exactly 5 cells
- every column must contain exactly 6 cells
- all cells must be equal size
- no merged cells
- no missing cells
- no extra cells
- no irregular row widths
- one sprite per cell
- one frame label at the bottom of each cell
- visible border lines between cells
- no title banner
- no extra decorations
- no extra animals
- no extra variants

Exact frame order:
Row 1: idle_00, idle_01, idle_02, idle_03, walk_00
Row 2: walk_01, walk_02, walk_03, walk_04, walk_05
Row 3: eat_00, eat_01, eat_02, eat_03, happy_00
Row 4: happy_01, happy_02, happy_03, sad_00, sad_01
Row 5: sleep_00, sleep_01, sick_00, sick_01, sick_02
Row 6: sick_03, bathe_00, bathe_01, bathe_02, bathe_03

Critical rules:
- exactly these 30 frames only
- do not skip frames
- do not duplicate frames
- do not invent extra walk frames
- do not rename any frame
- labels must exactly match the frame names above
- keep all cells evenly sized
- keep the sprite centered above the label in each cell
- keep the board as a rigid contact-sheet grid
- do not turn the board into a poster or model sheet

Style:
- pixel art
- crisp edges
- slightly moody, cute, vulnerable desktop-pet tone
- rounder than crow, puffed chest, shorter beak, awkward but lovable posture
- female should read slightly smaller and calmer
- yellow should be the dominant palette family without losing pigeon readability

Animation intent:
- idle: subtle breathing or blinking
- walk: readable side-view walk cycle
- eat: nibble or drink motion
- happy: excited positive motion
- sad: slumped low-energy pose
- sleep: clearly asleep and distinct from idle
- sick: visibly unwell and distinct from sad
- bathe: self-cleaning or washing motion

Return only the generated image.
```

## 82. pigeon / female / blue

```text
Generate an IMAGE ONLY.

Do not answer with text.
Do not describe limitations.
Do not provide configuration data.
Do not provide JSON.
Do not provide a table.
Do not provide a written explanation.

Create exactly one labeled sprite reference board image for this single variant only:

pigeon / female / blue

This prompt is self-contained. Do not rely on previous images.

Board rules:
- one single board only
- exactly 5 columns and exactly 6 rows
- exactly 30 total cells
- every row must contain exactly 5 cells
- every column must contain exactly 6 cells
- all cells must be equal size
- no merged cells
- no missing cells
- no extra cells
- no irregular row widths
- one sprite per cell
- one frame label at the bottom of each cell
- visible border lines between cells
- no title banner
- no extra decorations
- no extra animals
- no extra variants

Exact frame order:
Row 1: idle_00, idle_01, idle_02, idle_03, walk_00
Row 2: walk_01, walk_02, walk_03, walk_04, walk_05
Row 3: eat_00, eat_01, eat_02, eat_03, happy_00
Row 4: happy_01, happy_02, happy_03, sad_00, sad_01
Row 5: sleep_00, sleep_01, sick_00, sick_01, sick_02
Row 6: sick_03, bathe_00, bathe_01, bathe_02, bathe_03

Critical rules:
- exactly these 30 frames only
- do not skip frames
- do not duplicate frames
- do not invent extra walk frames
- do not rename any frame
- labels must exactly match the frame names above
- keep all cells evenly sized
- keep the sprite centered above the label in each cell
- keep the board as a rigid contact-sheet grid
- do not turn the board into a poster or model sheet

Style:
- pixel art
- crisp edges
- slightly moody, cute, vulnerable desktop-pet tone
- rounder than crow, puffed chest, shorter beak, awkward but lovable posture
- female should read slightly smaller and calmer
- blue should be the dominant palette family without losing pigeon readability

Animation intent:
- idle: subtle breathing or blinking
- walk: readable side-view walk cycle
- eat: nibble or drink motion
- happy: excited positive motion
- sad: slumped low-energy pose
- sleep: clearly asleep and distinct from idle
- sick: visibly unwell and distinct from sad
- bathe: self-cleaning or washing motion

Return only the generated image.
```

## 83. pigeon / female / indigo

```text
Generate an IMAGE ONLY.

Do not answer with text.
Do not describe limitations.
Do not provide configuration data.
Do not provide JSON.
Do not provide a table.
Do not provide a written explanation.

Create exactly one labeled sprite reference board image for this single variant only:

pigeon / female / indigo

This prompt is self-contained. Do not rely on previous images.

Board rules:
- one single board only
- exactly 5 columns and exactly 6 rows
- exactly 30 total cells
- every row must contain exactly 5 cells
- every column must contain exactly 6 cells
- all cells must be equal size
- no merged cells
- no missing cells
- no extra cells
- no irregular row widths
- one sprite per cell
- one frame label at the bottom of each cell
- visible border lines between cells
- no title banner
- no extra decorations
- no extra animals
- no extra variants

Exact frame order:
Row 1: idle_00, idle_01, idle_02, idle_03, walk_00
Row 2: walk_01, walk_02, walk_03, walk_04, walk_05
Row 3: eat_00, eat_01, eat_02, eat_03, happy_00
Row 4: happy_01, happy_02, happy_03, sad_00, sad_01
Row 5: sleep_00, sleep_01, sick_00, sick_01, sick_02
Row 6: sick_03, bathe_00, bathe_01, bathe_02, bathe_03

Critical rules:
- exactly these 30 frames only
- do not skip frames
- do not duplicate frames
- do not invent extra walk frames
- do not rename any frame
- labels must exactly match the frame names above
- keep all cells evenly sized
- keep the sprite centered above the label in each cell
- keep the board as a rigid contact-sheet grid
- do not turn the board into a poster or model sheet

Style:
- pixel art
- crisp edges
- slightly moody, cute, vulnerable desktop-pet tone
- rounder than crow, puffed chest, shorter beak, awkward but lovable posture
- female should read slightly smaller and calmer
- indigo should be the dominant palette family without losing pigeon readability

Animation intent:
- idle: subtle breathing or blinking
- walk: readable side-view walk cycle
- eat: nibble or drink motion
- happy: excited positive motion
- sad: slumped low-energy pose
- sleep: clearly asleep and distinct from idle
- sick: visibly unwell and distinct from sad
- bathe: self-cleaning or washing motion

Return only the generated image.
```

## 84. pigeon / female / violet

```text
Generate an IMAGE ONLY.

Do not answer with text.
Do not describe limitations.
Do not provide configuration data.
Do not provide JSON.
Do not provide a table.
Do not provide a written explanation.

Create exactly one labeled sprite reference board image for this single variant only:

pigeon / female / violet

This prompt is self-contained. Do not rely on previous images.

Board rules:
- one single board only
- exactly 5 columns and exactly 6 rows
- exactly 30 total cells
- every row must contain exactly 5 cells
- every column must contain exactly 6 cells
- all cells must be equal size
- no merged cells
- no missing cells
- no extra cells
- no irregular row widths
- one sprite per cell
- one frame label at the bottom of each cell
- visible border lines between cells
- no title banner
- no extra decorations
- no extra animals
- no extra variants

Exact frame order:
Row 1: idle_00, idle_01, idle_02, idle_03, walk_00
Row 2: walk_01, walk_02, walk_03, walk_04, walk_05
Row 3: eat_00, eat_01, eat_02, eat_03, happy_00
Row 4: happy_01, happy_02, happy_03, sad_00, sad_01
Row 5: sleep_00, sleep_01, sick_00, sick_01, sick_02
Row 6: sick_03, bathe_00, bathe_01, bathe_02, bathe_03

Critical rules:
- exactly these 30 frames only
- do not skip frames
- do not duplicate frames
- do not invent extra walk frames
- do not rename any frame
- labels must exactly match the frame names above
- keep all cells evenly sized
- keep the sprite centered above the label in each cell
- keep the board as a rigid contact-sheet grid
- do not turn the board into a poster or model sheet

Style:
- pixel art
- crisp edges
- slightly moody, cute, vulnerable desktop-pet tone
- rounder than crow, puffed chest, shorter beak, awkward but lovable posture
- female should read slightly smaller and calmer
- violet should be the dominant palette family without losing pigeon readability

Animation intent:
- idle: subtle breathing or blinking
- walk: readable side-view walk cycle
- eat: nibble or drink motion
- happy: excited positive motion
- sad: slumped low-energy pose
- sleep: clearly asleep and distinct from idle
- sick: visibly unwell and distinct from sad
- bathe: self-cleaning or washing motion

Return only the generated image.
```

## 85. raccoon / male / red

```text
Generate an IMAGE ONLY.

Do not answer with text.
Do not describe limitations.
Do not provide configuration data.
Do not provide JSON.
Do not provide a table.
Do not provide a written explanation.

Create exactly one labeled sprite reference board image for this single variant only:

raccoon / male / red

This prompt is self-contained. Do not rely on previous images.

Board rules:
- one single board only
- exactly 5 columns and exactly 6 rows
- exactly 30 total cells
- every row must contain exactly 5 cells
- every column must contain exactly 6 cells
- all cells must be equal size
- no merged cells
- no missing cells
- no extra cells
- no irregular row widths
- one sprite per cell
- one frame label at the bottom of each cell
- visible border lines between cells
- no title banner
- no extra decorations
- no extra animals
- no extra variants

Exact frame order:
Row 1: idle_00, idle_01, idle_02, idle_03, walk_00
Row 2: walk_01, walk_02, walk_03, walk_04, walk_05
Row 3: eat_00, eat_01, eat_02, eat_03, happy_00
Row 4: happy_01, happy_02, happy_03, sad_00, sad_01
Row 5: sleep_00, sleep_01, sick_00, sick_01, sick_02
Row 6: sick_03, bathe_00, bathe_01, bathe_02, bathe_03

Critical rules:
- exactly these 30 frames only
- do not skip frames
- do not duplicate frames
- do not invent extra walk frames
- do not rename any frame
- labels must exactly match the frame names above
- keep all cells evenly sized
- keep the sprite centered above the label in each cell
- keep the board as a rigid contact-sheet grid
- do not turn the board into a poster or model sheet

Style:
- pixel art
- crisp edges
- slightly moody, cute, vulnerable desktop-pet tone
- masked face, ringed tail, scrappy silhouette
- male should read slightly larger and bolder
- red should be the dominant palette family without losing raccoon readability

Animation intent:
- idle: subtle breathing or blinking
- walk: readable side-view walk cycle
- eat: nibble or drink motion
- happy: excited positive motion
- sad: slumped low-energy pose
- sleep: clearly asleep and distinct from idle
- sick: visibly unwell and distinct from sad
- bathe: self-cleaning or washing motion

Return only the generated image.
```

## 86. raccoon / male / orange

```text
Generate an IMAGE ONLY.

Do not answer with text.
Do not describe limitations.
Do not provide configuration data.
Do not provide JSON.
Do not provide a table.
Do not provide a written explanation.

Create exactly one labeled sprite reference board image for this single variant only:

raccoon / male / orange

This prompt is self-contained. Do not rely on previous images.

Board rules:
- one single board only
- exactly 5 columns and exactly 6 rows
- exactly 30 total cells
- every row must contain exactly 5 cells
- every column must contain exactly 6 cells
- all cells must be equal size
- no merged cells
- no missing cells
- no extra cells
- no irregular row widths
- one sprite per cell
- one frame label at the bottom of each cell
- visible border lines between cells
- no title banner
- no extra decorations
- no extra animals
- no extra variants

Exact frame order:
Row 1: idle_00, idle_01, idle_02, idle_03, walk_00
Row 2: walk_01, walk_02, walk_03, walk_04, walk_05
Row 3: eat_00, eat_01, eat_02, eat_03, happy_00
Row 4: happy_01, happy_02, happy_03, sad_00, sad_01
Row 5: sleep_00, sleep_01, sick_00, sick_01, sick_02
Row 6: sick_03, bathe_00, bathe_01, bathe_02, bathe_03

Critical rules:
- exactly these 30 frames only
- do not skip frames
- do not duplicate frames
- do not invent extra walk frames
- do not rename any frame
- labels must exactly match the frame names above
- keep all cells evenly sized
- keep the sprite centered above the label in each cell
- keep the board as a rigid contact-sheet grid
- do not turn the board into a poster or model sheet

Style:
- pixel art
- crisp edges
- slightly moody, cute, vulnerable desktop-pet tone
- masked face, ringed tail, scrappy silhouette
- male should read slightly larger and bolder
- orange should be the dominant palette family without losing raccoon readability

Animation intent:
- idle: subtle breathing or blinking
- walk: readable side-view walk cycle
- eat: nibble or drink motion
- happy: excited positive motion
- sad: slumped low-energy pose
- sleep: clearly asleep and distinct from idle
- sick: visibly unwell and distinct from sad
- bathe: self-cleaning or washing motion

Return only the generated image.
```

## 87. raccoon / male / yellow

```text
Generate an IMAGE ONLY.

Do not answer with text.
Do not describe limitations.
Do not provide configuration data.
Do not provide JSON.
Do not provide a table.
Do not provide a written explanation.

Create exactly one labeled sprite reference board image for this single variant only:

raccoon / male / yellow

This prompt is self-contained. Do not rely on previous images.

Board rules:
- one single board only
- exactly 5 columns and exactly 6 rows
- exactly 30 total cells
- every row must contain exactly 5 cells
- every column must contain exactly 6 cells
- all cells must be equal size
- no merged cells
- no missing cells
- no extra cells
- no irregular row widths
- one sprite per cell
- one frame label at the bottom of each cell
- visible border lines between cells
- no title banner
- no extra decorations
- no extra animals
- no extra variants

Exact frame order:
Row 1: idle_00, idle_01, idle_02, idle_03, walk_00
Row 2: walk_01, walk_02, walk_03, walk_04, walk_05
Row 3: eat_00, eat_01, eat_02, eat_03, happy_00
Row 4: happy_01, happy_02, happy_03, sad_00, sad_01
Row 5: sleep_00, sleep_01, sick_00, sick_01, sick_02
Row 6: sick_03, bathe_00, bathe_01, bathe_02, bathe_03

Critical rules:
- exactly these 30 frames only
- do not skip frames
- do not duplicate frames
- do not invent extra walk frames
- do not rename any frame
- labels must exactly match the frame names above
- keep all cells evenly sized
- keep the sprite centered above the label in each cell
- keep the board as a rigid contact-sheet grid
- do not turn the board into a poster or model sheet

Style:
- pixel art
- crisp edges
- slightly moody, cute, vulnerable desktop-pet tone
- masked face, ringed tail, scrappy silhouette
- male should read slightly larger and bolder
- yellow should be the dominant palette family without losing raccoon readability

Animation intent:
- idle: subtle breathing or blinking
- walk: readable side-view walk cycle
- eat: nibble or drink motion
- happy: excited positive motion
- sad: slumped low-energy pose
- sleep: clearly asleep and distinct from idle
- sick: visibly unwell and distinct from sad
- bathe: self-cleaning or washing motion

Return only the generated image.
```

## 88. raccoon / male / blue

```text
Generate an IMAGE ONLY.

Do not answer with text.
Do not describe limitations.
Do not provide configuration data.
Do not provide JSON.
Do not provide a table.
Do not provide a written explanation.

Create exactly one labeled sprite reference board image for this single variant only:

raccoon / male / blue

This prompt is self-contained. Do not rely on previous images.

Board rules:
- one single board only
- exactly 5 columns and exactly 6 rows
- exactly 30 total cells
- every row must contain exactly 5 cells
- every column must contain exactly 6 cells
- all cells must be equal size
- no merged cells
- no missing cells
- no extra cells
- no irregular row widths
- one sprite per cell
- one frame label at the bottom of each cell
- visible border lines between cells
- no title banner
- no extra decorations
- no extra animals
- no extra variants

Exact frame order:
Row 1: idle_00, idle_01, idle_02, idle_03, walk_00
Row 2: walk_01, walk_02, walk_03, walk_04, walk_05
Row 3: eat_00, eat_01, eat_02, eat_03, happy_00
Row 4: happy_01, happy_02, happy_03, sad_00, sad_01
Row 5: sleep_00, sleep_01, sick_00, sick_01, sick_02
Row 6: sick_03, bathe_00, bathe_01, bathe_02, bathe_03

Critical rules:
- exactly these 30 frames only
- do not skip frames
- do not duplicate frames
- do not invent extra walk frames
- do not rename any frame
- labels must exactly match the frame names above
- keep all cells evenly sized
- keep the sprite centered above the label in each cell
- keep the board as a rigid contact-sheet grid
- do not turn the board into a poster or model sheet

Style:
- pixel art
- crisp edges
- slightly moody, cute, vulnerable desktop-pet tone
- masked face, ringed tail, scrappy silhouette
- male should read slightly larger and bolder
- blue should be the dominant palette family without losing raccoon readability

Animation intent:
- idle: subtle breathing or blinking
- walk: readable side-view walk cycle
- eat: nibble or drink motion
- happy: excited positive motion
- sad: slumped low-energy pose
- sleep: clearly asleep and distinct from idle
- sick: visibly unwell and distinct from sad
- bathe: self-cleaning or washing motion

Return only the generated image.
```

## 89. raccoon / male / indigo

```text
Generate an IMAGE ONLY.

Do not answer with text.
Do not describe limitations.
Do not provide configuration data.
Do not provide JSON.
Do not provide a table.
Do not provide a written explanation.

Create exactly one labeled sprite reference board image for this single variant only:

raccoon / male / indigo

This prompt is self-contained. Do not rely on previous images.

Board rules:
- one single board only
- exactly 5 columns and exactly 6 rows
- exactly 30 total cells
- every row must contain exactly 5 cells
- every column must contain exactly 6 cells
- all cells must be equal size
- no merged cells
- no missing cells
- no extra cells
- no irregular row widths
- one sprite per cell
- one frame label at the bottom of each cell
- visible border lines between cells
- no title banner
- no extra decorations
- no extra animals
- no extra variants

Exact frame order:
Row 1: idle_00, idle_01, idle_02, idle_03, walk_00
Row 2: walk_01, walk_02, walk_03, walk_04, walk_05
Row 3: eat_00, eat_01, eat_02, eat_03, happy_00
Row 4: happy_01, happy_02, happy_03, sad_00, sad_01
Row 5: sleep_00, sleep_01, sick_00, sick_01, sick_02
Row 6: sick_03, bathe_00, bathe_01, bathe_02, bathe_03

Critical rules:
- exactly these 30 frames only
- do not skip frames
- do not duplicate frames
- do not invent extra walk frames
- do not rename any frame
- labels must exactly match the frame names above
- keep all cells evenly sized
- keep the sprite centered above the label in each cell
- keep the board as a rigid contact-sheet grid
- do not turn the board into a poster or model sheet

Style:
- pixel art
- crisp edges
- slightly moody, cute, vulnerable desktop-pet tone
- masked face, ringed tail, scrappy silhouette
- male should read slightly larger and bolder
- indigo should be the dominant palette family without losing raccoon readability

Animation intent:
- idle: subtle breathing or blinking
- walk: readable side-view walk cycle
- eat: nibble or drink motion
- happy: excited positive motion
- sad: slumped low-energy pose
- sleep: clearly asleep and distinct from idle
- sick: visibly unwell and distinct from sad
- bathe: self-cleaning or washing motion

Return only the generated image.
```

## 90. raccoon / male / violet

```text
Generate an IMAGE ONLY.

Do not answer with text.
Do not describe limitations.
Do not provide configuration data.
Do not provide JSON.
Do not provide a table.
Do not provide a written explanation.

Create exactly one labeled sprite reference board image for this single variant only:

raccoon / male / violet

This prompt is self-contained. Do not rely on previous images.

Board rules:
- one single board only
- exactly 5 columns and exactly 6 rows
- exactly 30 total cells
- every row must contain exactly 5 cells
- every column must contain exactly 6 cells
- all cells must be equal size
- no merged cells
- no missing cells
- no extra cells
- no irregular row widths
- one sprite per cell
- one frame label at the bottom of each cell
- visible border lines between cells
- no title banner
- no extra decorations
- no extra animals
- no extra variants

Exact frame order:
Row 1: idle_00, idle_01, idle_02, idle_03, walk_00
Row 2: walk_01, walk_02, walk_03, walk_04, walk_05
Row 3: eat_00, eat_01, eat_02, eat_03, happy_00
Row 4: happy_01, happy_02, happy_03, sad_00, sad_01
Row 5: sleep_00, sleep_01, sick_00, sick_01, sick_02
Row 6: sick_03, bathe_00, bathe_01, bathe_02, bathe_03

Critical rules:
- exactly these 30 frames only
- do not skip frames
- do not duplicate frames
- do not invent extra walk frames
- do not rename any frame
- labels must exactly match the frame names above
- keep all cells evenly sized
- keep the sprite centered above the label in each cell
- keep the board as a rigid contact-sheet grid
- do not turn the board into a poster or model sheet

Style:
- pixel art
- crisp edges
- slightly moody, cute, vulnerable desktop-pet tone
- masked face, ringed tail, scrappy silhouette
- male should read slightly larger and bolder
- violet should be the dominant palette family without losing raccoon readability

Animation intent:
- idle: subtle breathing or blinking
- walk: readable side-view walk cycle
- eat: nibble or drink motion
- happy: excited positive motion
- sad: slumped low-energy pose
- sleep: clearly asleep and distinct from idle
- sick: visibly unwell and distinct from sad
- bathe: self-cleaning or washing motion

Return only the generated image.
```

## 91. raccoon / female / red

```text
Generate an IMAGE ONLY.

Do not answer with text.
Do not describe limitations.
Do not provide configuration data.
Do not provide JSON.
Do not provide a table.
Do not provide a written explanation.

Create exactly one labeled sprite reference board image for this single variant only:

raccoon / female / red

This prompt is self-contained. Do not rely on previous images.

Board rules:
- one single board only
- exactly 5 columns and exactly 6 rows
- exactly 30 total cells
- every row must contain exactly 5 cells
- every column must contain exactly 6 cells
- all cells must be equal size
- no merged cells
- no missing cells
- no extra cells
- no irregular row widths
- one sprite per cell
- one frame label at the bottom of each cell
- visible border lines between cells
- no title banner
- no extra decorations
- no extra animals
- no extra variants

Exact frame order:
Row 1: idle_00, idle_01, idle_02, idle_03, walk_00
Row 2: walk_01, walk_02, walk_03, walk_04, walk_05
Row 3: eat_00, eat_01, eat_02, eat_03, happy_00
Row 4: happy_01, happy_02, happy_03, sad_00, sad_01
Row 5: sleep_00, sleep_01, sick_00, sick_01, sick_02
Row 6: sick_03, bathe_00, bathe_01, bathe_02, bathe_03

Critical rules:
- exactly these 30 frames only
- do not skip frames
- do not duplicate frames
- do not invent extra walk frames
- do not rename any frame
- labels must exactly match the frame names above
- keep all cells evenly sized
- keep the sprite centered above the label in each cell
- keep the board as a rigid contact-sheet grid
- do not turn the board into a poster or model sheet

Style:
- pixel art
- crisp edges
- slightly moody, cute, vulnerable desktop-pet tone
- masked face, ringed tail, scrappy silhouette
- female should read slightly smaller and calmer
- red should be the dominant palette family without losing raccoon readability

Animation intent:
- idle: subtle breathing or blinking
- walk: readable side-view walk cycle
- eat: nibble or drink motion
- happy: excited positive motion
- sad: slumped low-energy pose
- sleep: clearly asleep and distinct from idle
- sick: visibly unwell and distinct from sad
- bathe: self-cleaning or washing motion

Return only the generated image.
```

## 92. raccoon / female / orange

```text
Generate an IMAGE ONLY.

Do not answer with text.
Do not describe limitations.
Do not provide configuration data.
Do not provide JSON.
Do not provide a table.
Do not provide a written explanation.

Create exactly one labeled sprite reference board image for this single variant only:

raccoon / female / orange

This prompt is self-contained. Do not rely on previous images.

Board rules:
- one single board only
- exactly 5 columns and exactly 6 rows
- exactly 30 total cells
- every row must contain exactly 5 cells
- every column must contain exactly 6 cells
- all cells must be equal size
- no merged cells
- no missing cells
- no extra cells
- no irregular row widths
- one sprite per cell
- one frame label at the bottom of each cell
- visible border lines between cells
- no title banner
- no extra decorations
- no extra animals
- no extra variants

Exact frame order:
Row 1: idle_00, idle_01, idle_02, idle_03, walk_00
Row 2: walk_01, walk_02, walk_03, walk_04, walk_05
Row 3: eat_00, eat_01, eat_02, eat_03, happy_00
Row 4: happy_01, happy_02, happy_03, sad_00, sad_01
Row 5: sleep_00, sleep_01, sick_00, sick_01, sick_02
Row 6: sick_03, bathe_00, bathe_01, bathe_02, bathe_03

Critical rules:
- exactly these 30 frames only
- do not skip frames
- do not duplicate frames
- do not invent extra walk frames
- do not rename any frame
- labels must exactly match the frame names above
- keep all cells evenly sized
- keep the sprite centered above the label in each cell
- keep the board as a rigid contact-sheet grid
- do not turn the board into a poster or model sheet

Style:
- pixel art
- crisp edges
- slightly moody, cute, vulnerable desktop-pet tone
- masked face, ringed tail, scrappy silhouette
- female should read slightly smaller and calmer
- orange should be the dominant palette family without losing raccoon readability

Animation intent:
- idle: subtle breathing or blinking
- walk: readable side-view walk cycle
- eat: nibble or drink motion
- happy: excited positive motion
- sad: slumped low-energy pose
- sleep: clearly asleep and distinct from idle
- sick: visibly unwell and distinct from sad
- bathe: self-cleaning or washing motion

Return only the generated image.
```

## 93. raccoon / female / yellow

```text
Generate an IMAGE ONLY.

Do not answer with text.
Do not describe limitations.
Do not provide configuration data.
Do not provide JSON.
Do not provide a table.
Do not provide a written explanation.

Create exactly one labeled sprite reference board image for this single variant only:

raccoon / female / yellow

This prompt is self-contained. Do not rely on previous images.

Board rules:
- one single board only
- exactly 5 columns and exactly 6 rows
- exactly 30 total cells
- every row must contain exactly 5 cells
- every column must contain exactly 6 cells
- all cells must be equal size
- no merged cells
- no missing cells
- no extra cells
- no irregular row widths
- one sprite per cell
- one frame label at the bottom of each cell
- visible border lines between cells
- no title banner
- no extra decorations
- no extra animals
- no extra variants

Exact frame order:
Row 1: idle_00, idle_01, idle_02, idle_03, walk_00
Row 2: walk_01, walk_02, walk_03, walk_04, walk_05
Row 3: eat_00, eat_01, eat_02, eat_03, happy_00
Row 4: happy_01, happy_02, happy_03, sad_00, sad_01
Row 5: sleep_00, sleep_01, sick_00, sick_01, sick_02
Row 6: sick_03, bathe_00, bathe_01, bathe_02, bathe_03

Critical rules:
- exactly these 30 frames only
- do not skip frames
- do not duplicate frames
- do not invent extra walk frames
- do not rename any frame
- labels must exactly match the frame names above
- keep all cells evenly sized
- keep the sprite centered above the label in each cell
- keep the board as a rigid contact-sheet grid
- do not turn the board into a poster or model sheet

Style:
- pixel art
- crisp edges
- slightly moody, cute, vulnerable desktop-pet tone
- masked face, ringed tail, scrappy silhouette
- female should read slightly smaller and calmer
- yellow should be the dominant palette family without losing raccoon readability

Animation intent:
- idle: subtle breathing or blinking
- walk: readable side-view walk cycle
- eat: nibble or drink motion
- happy: excited positive motion
- sad: slumped low-energy pose
- sleep: clearly asleep and distinct from idle
- sick: visibly unwell and distinct from sad
- bathe: self-cleaning or washing motion

Return only the generated image.
```

## 94. raccoon / female / blue

```text
Generate an IMAGE ONLY.

Do not answer with text.
Do not describe limitations.
Do not provide configuration data.
Do not provide JSON.
Do not provide a table.
Do not provide a written explanation.

Create exactly one labeled sprite reference board image for this single variant only:

raccoon / female / blue

This prompt is self-contained. Do not rely on previous images.

Board rules:
- one single board only
- exactly 5 columns and exactly 6 rows
- exactly 30 total cells
- every row must contain exactly 5 cells
- every column must contain exactly 6 cells
- all cells must be equal size
- no merged cells
- no missing cells
- no extra cells
- no irregular row widths
- one sprite per cell
- one frame label at the bottom of each cell
- visible border lines between cells
- no title banner
- no extra decorations
- no extra animals
- no extra variants

Exact frame order:
Row 1: idle_00, idle_01, idle_02, idle_03, walk_00
Row 2: walk_01, walk_02, walk_03, walk_04, walk_05
Row 3: eat_00, eat_01, eat_02, eat_03, happy_00
Row 4: happy_01, happy_02, happy_03, sad_00, sad_01
Row 5: sleep_00, sleep_01, sick_00, sick_01, sick_02
Row 6: sick_03, bathe_00, bathe_01, bathe_02, bathe_03

Critical rules:
- exactly these 30 frames only
- do not skip frames
- do not duplicate frames
- do not invent extra walk frames
- do not rename any frame
- labels must exactly match the frame names above
- keep all cells evenly sized
- keep the sprite centered above the label in each cell
- keep the board as a rigid contact-sheet grid
- do not turn the board into a poster or model sheet

Style:
- pixel art
- crisp edges
- slightly moody, cute, vulnerable desktop-pet tone
- masked face, ringed tail, scrappy silhouette
- female should read slightly smaller and calmer
- blue should be the dominant palette family without losing raccoon readability

Animation intent:
- idle: subtle breathing or blinking
- walk: readable side-view walk cycle
- eat: nibble or drink motion
- happy: excited positive motion
- sad: slumped low-energy pose
- sleep: clearly asleep and distinct from idle
- sick: visibly unwell and distinct from sad
- bathe: self-cleaning or washing motion

Return only the generated image.
```

## 95. raccoon / female / indigo

```text
Generate an IMAGE ONLY.

Do not answer with text.
Do not describe limitations.
Do not provide configuration data.
Do not provide JSON.
Do not provide a table.
Do not provide a written explanation.

Create exactly one labeled sprite reference board image for this single variant only:

raccoon / female / indigo

This prompt is self-contained. Do not rely on previous images.

Board rules:
- one single board only
- exactly 5 columns and exactly 6 rows
- exactly 30 total cells
- every row must contain exactly 5 cells
- every column must contain exactly 6 cells
- all cells must be equal size
- no merged cells
- no missing cells
- no extra cells
- no irregular row widths
- one sprite per cell
- one frame label at the bottom of each cell
- visible border lines between cells
- no title banner
- no extra decorations
- no extra animals
- no extra variants

Exact frame order:
Row 1: idle_00, idle_01, idle_02, idle_03, walk_00
Row 2: walk_01, walk_02, walk_03, walk_04, walk_05
Row 3: eat_00, eat_01, eat_02, eat_03, happy_00
Row 4: happy_01, happy_02, happy_03, sad_00, sad_01
Row 5: sleep_00, sleep_01, sick_00, sick_01, sick_02
Row 6: sick_03, bathe_00, bathe_01, bathe_02, bathe_03

Critical rules:
- exactly these 30 frames only
- do not skip frames
- do not duplicate frames
- do not invent extra walk frames
- do not rename any frame
- labels must exactly match the frame names above
- keep all cells evenly sized
- keep the sprite centered above the label in each cell
- keep the board as a rigid contact-sheet grid
- do not turn the board into a poster or model sheet

Style:
- pixel art
- crisp edges
- slightly moody, cute, vulnerable desktop-pet tone
- masked face, ringed tail, scrappy silhouette
- female should read slightly smaller and calmer
- indigo should be the dominant palette family without losing raccoon readability

Animation intent:
- idle: subtle breathing or blinking
- walk: readable side-view walk cycle
- eat: nibble or drink motion
- happy: excited positive motion
- sad: slumped low-energy pose
- sleep: clearly asleep and distinct from idle
- sick: visibly unwell and distinct from sad
- bathe: self-cleaning or washing motion

Return only the generated image.
```

## 96. raccoon / female / violet

```text
Generate an IMAGE ONLY.

Do not answer with text.
Do not describe limitations.
Do not provide configuration data.
Do not provide JSON.
Do not provide a table.
Do not provide a written explanation.

Create exactly one labeled sprite reference board image for this single variant only:

raccoon / female / violet

This prompt is self-contained. Do not rely on previous images.

Board rules:
- one single board only
- exactly 5 columns and exactly 6 rows
- exactly 30 total cells
- every row must contain exactly 5 cells
- every column must contain exactly 6 cells
- all cells must be equal size
- no merged cells
- no missing cells
- no extra cells
- no irregular row widths
- one sprite per cell
- one frame label at the bottom of each cell
- visible border lines between cells
- no title banner
- no extra decorations
- no extra animals
- no extra variants

Exact frame order:
Row 1: idle_00, idle_01, idle_02, idle_03, walk_00
Row 2: walk_01, walk_02, walk_03, walk_04, walk_05
Row 3: eat_00, eat_01, eat_02, eat_03, happy_00
Row 4: happy_01, happy_02, happy_03, sad_00, sad_01
Row 5: sleep_00, sleep_01, sick_00, sick_01, sick_02
Row 6: sick_03, bathe_00, bathe_01, bathe_02, bathe_03

Critical rules:
- exactly these 30 frames only
- do not skip frames
- do not duplicate frames
- do not invent extra walk frames
- do not rename any frame
- labels must exactly match the frame names above
- keep all cells evenly sized
- keep the sprite centered above the label in each cell
- keep the board as a rigid contact-sheet grid
- do not turn the board into a poster or model sheet

Style:
- pixel art
- crisp edges
- slightly moody, cute, vulnerable desktop-pet tone
- masked face, ringed tail, scrappy silhouette
- female should read slightly smaller and calmer
- violet should be the dominant palette family without losing raccoon readability

Animation intent:
- idle: subtle breathing or blinking
- walk: readable side-view walk cycle
- eat: nibble or drink motion
- happy: excited positive motion
- sad: slumped low-energy pose
- sleep: clearly asleep and distinct from idle
- sick: visibly unwell and distinct from sad
- bathe: self-cleaning or washing motion

Return only the generated image.
```

## 97. squirrel / male / red

```text
Generate an IMAGE ONLY.

Do not answer with text.
Do not describe limitations.
Do not provide configuration data.
Do not provide JSON.
Do not provide a table.
Do not provide a written explanation.

Create exactly one labeled sprite reference board image for this single variant only:

squirrel / male / red

This prompt is self-contained. Do not rely on previous images.

Board rules:
- one single board only
- exactly 5 columns and exactly 6 rows
- exactly 30 total cells
- every row must contain exactly 5 cells
- every column must contain exactly 6 cells
- all cells must be equal size
- no merged cells
- no missing cells
- no extra cells
- no irregular row widths
- one sprite per cell
- one frame label at the bottom of each cell
- visible border lines between cells
- no title banner
- no extra decorations
- no extra animals
- no extra variants

Exact frame order:
Row 1: idle_00, idle_01, idle_02, idle_03, walk_00
Row 2: walk_01, walk_02, walk_03, walk_04, walk_05
Row 3: eat_00, eat_01, eat_02, eat_03, happy_00
Row 4: happy_01, happy_02, happy_03, sad_00, sad_01
Row 5: sleep_00, sleep_01, sick_00, sick_01, sick_02
Row 6: sick_03, bathe_00, bathe_01, bathe_02, bathe_03

Critical rules:
- exactly these 30 frames only
- do not skip frames
- do not duplicate frames
- do not invent extra walk frames
- do not rename any frame
- labels must exactly match the frame names above
- keep all cells evenly sized
- keep the sprite centered above the label in each cell
- keep the board as a rigid contact-sheet grid
- do not turn the board into a poster or model sheet

Style:
- pixel art
- crisp edges
- slightly moody, cute, vulnerable desktop-pet tone
- upright body, bushy tail essential, nimble and alert
- male should read slightly larger and bolder
- red should be the dominant palette family without losing squirrel readability

Animation intent:
- idle: subtle breathing or blinking
- walk: readable side-view walk cycle
- eat: nibble or drink motion
- happy: excited positive motion
- sad: slumped low-energy pose
- sleep: clearly asleep and distinct from idle
- sick: visibly unwell and distinct from sad
- bathe: self-cleaning or washing motion

Return only the generated image.
```

## 98. squirrel / male / orange

```text
Generate an IMAGE ONLY.

Do not answer with text.
Do not describe limitations.
Do not provide configuration data.
Do not provide JSON.
Do not provide a table.
Do not provide a written explanation.

Create exactly one labeled sprite reference board image for this single variant only:

squirrel / male / orange

This prompt is self-contained. Do not rely on previous images.

Board rules:
- one single board only
- exactly 5 columns and exactly 6 rows
- exactly 30 total cells
- every row must contain exactly 5 cells
- every column must contain exactly 6 cells
- all cells must be equal size
- no merged cells
- no missing cells
- no extra cells
- no irregular row widths
- one sprite per cell
- one frame label at the bottom of each cell
- visible border lines between cells
- no title banner
- no extra decorations
- no extra animals
- no extra variants

Exact frame order:
Row 1: idle_00, idle_01, idle_02, idle_03, walk_00
Row 2: walk_01, walk_02, walk_03, walk_04, walk_05
Row 3: eat_00, eat_01, eat_02, eat_03, happy_00
Row 4: happy_01, happy_02, happy_03, sad_00, sad_01
Row 5: sleep_00, sleep_01, sick_00, sick_01, sick_02
Row 6: sick_03, bathe_00, bathe_01, bathe_02, bathe_03

Critical rules:
- exactly these 30 frames only
- do not skip frames
- do not duplicate frames
- do not invent extra walk frames
- do not rename any frame
- labels must exactly match the frame names above
- keep all cells evenly sized
- keep the sprite centered above the label in each cell
- keep the board as a rigid contact-sheet grid
- do not turn the board into a poster or model sheet

Style:
- pixel art
- crisp edges
- slightly moody, cute, vulnerable desktop-pet tone
- upright body, bushy tail essential, nimble and alert
- male should read slightly larger and bolder
- orange should be the dominant palette family without losing squirrel readability

Animation intent:
- idle: subtle breathing or blinking
- walk: readable side-view walk cycle
- eat: nibble or drink motion
- happy: excited positive motion
- sad: slumped low-energy pose
- sleep: clearly asleep and distinct from idle
- sick: visibly unwell and distinct from sad
- bathe: self-cleaning or washing motion

Return only the generated image.
```

## 99. squirrel / male / yellow

```text
Generate an IMAGE ONLY.

Do not answer with text.
Do not describe limitations.
Do not provide configuration data.
Do not provide JSON.
Do not provide a table.
Do not provide a written explanation.

Create exactly one labeled sprite reference board image for this single variant only:

squirrel / male / yellow

This prompt is self-contained. Do not rely on previous images.

Board rules:
- one single board only
- exactly 5 columns and exactly 6 rows
- exactly 30 total cells
- every row must contain exactly 5 cells
- every column must contain exactly 6 cells
- all cells must be equal size
- no merged cells
- no missing cells
- no extra cells
- no irregular row widths
- one sprite per cell
- one frame label at the bottom of each cell
- visible border lines between cells
- no title banner
- no extra decorations
- no extra animals
- no extra variants

Exact frame order:
Row 1: idle_00, idle_01, idle_02, idle_03, walk_00
Row 2: walk_01, walk_02, walk_03, walk_04, walk_05
Row 3: eat_00, eat_01, eat_02, eat_03, happy_00
Row 4: happy_01, happy_02, happy_03, sad_00, sad_01
Row 5: sleep_00, sleep_01, sick_00, sick_01, sick_02
Row 6: sick_03, bathe_00, bathe_01, bathe_02, bathe_03

Critical rules:
- exactly these 30 frames only
- do not skip frames
- do not duplicate frames
- do not invent extra walk frames
- do not rename any frame
- labels must exactly match the frame names above
- keep all cells evenly sized
- keep the sprite centered above the label in each cell
- keep the board as a rigid contact-sheet grid
- do not turn the board into a poster or model sheet

Style:
- pixel art
- crisp edges
- slightly moody, cute, vulnerable desktop-pet tone
- upright body, bushy tail essential, nimble and alert
- male should read slightly larger and bolder
- yellow should be the dominant palette family without losing squirrel readability

Animation intent:
- idle: subtle breathing or blinking
- walk: readable side-view walk cycle
- eat: nibble or drink motion
- happy: excited positive motion
- sad: slumped low-energy pose
- sleep: clearly asleep and distinct from idle
- sick: visibly unwell and distinct from sad
- bathe: self-cleaning or washing motion

Return only the generated image.
```

## 100. squirrel / male / blue

```text
Generate an IMAGE ONLY.

Do not answer with text.
Do not describe limitations.
Do not provide configuration data.
Do not provide JSON.
Do not provide a table.
Do not provide a written explanation.

Create exactly one labeled sprite reference board image for this single variant only:

squirrel / male / blue

This prompt is self-contained. Do not rely on previous images.

Board rules:
- one single board only
- exactly 5 columns and exactly 6 rows
- exactly 30 total cells
- every row must contain exactly 5 cells
- every column must contain exactly 6 cells
- all cells must be equal size
- no merged cells
- no missing cells
- no extra cells
- no irregular row widths
- one sprite per cell
- one frame label at the bottom of each cell
- visible border lines between cells
- no title banner
- no extra decorations
- no extra animals
- no extra variants

Exact frame order:
Row 1: idle_00, idle_01, idle_02, idle_03, walk_00
Row 2: walk_01, walk_02, walk_03, walk_04, walk_05
Row 3: eat_00, eat_01, eat_02, eat_03, happy_00
Row 4: happy_01, happy_02, happy_03, sad_00, sad_01
Row 5: sleep_00, sleep_01, sick_00, sick_01, sick_02
Row 6: sick_03, bathe_00, bathe_01, bathe_02, bathe_03

Critical rules:
- exactly these 30 frames only
- do not skip frames
- do not duplicate frames
- do not invent extra walk frames
- do not rename any frame
- labels must exactly match the frame names above
- keep all cells evenly sized
- keep the sprite centered above the label in each cell
- keep the board as a rigid contact-sheet grid
- do not turn the board into a poster or model sheet

Style:
- pixel art
- crisp edges
- slightly moody, cute, vulnerable desktop-pet tone
- upright body, bushy tail essential, nimble and alert
- male should read slightly larger and bolder
- blue should be the dominant palette family without losing squirrel readability

Animation intent:
- idle: subtle breathing or blinking
- walk: readable side-view walk cycle
- eat: nibble or drink motion
- happy: excited positive motion
- sad: slumped low-energy pose
- sleep: clearly asleep and distinct from idle
- sick: visibly unwell and distinct from sad
- bathe: self-cleaning or washing motion

Return only the generated image.
```

## 101. squirrel / male / indigo

```text
Generate an IMAGE ONLY.

Do not answer with text.
Do not describe limitations.
Do not provide configuration data.
Do not provide JSON.
Do not provide a table.
Do not provide a written explanation.

Create exactly one labeled sprite reference board image for this single variant only:

squirrel / male / indigo

This prompt is self-contained. Do not rely on previous images.

Board rules:
- one single board only
- exactly 5 columns and exactly 6 rows
- exactly 30 total cells
- every row must contain exactly 5 cells
- every column must contain exactly 6 cells
- all cells must be equal size
- no merged cells
- no missing cells
- no extra cells
- no irregular row widths
- one sprite per cell
- one frame label at the bottom of each cell
- visible border lines between cells
- no title banner
- no extra decorations
- no extra animals
- no extra variants

Exact frame order:
Row 1: idle_00, idle_01, idle_02, idle_03, walk_00
Row 2: walk_01, walk_02, walk_03, walk_04, walk_05
Row 3: eat_00, eat_01, eat_02, eat_03, happy_00
Row 4: happy_01, happy_02, happy_03, sad_00, sad_01
Row 5: sleep_00, sleep_01, sick_00, sick_01, sick_02
Row 6: sick_03, bathe_00, bathe_01, bathe_02, bathe_03

Critical rules:
- exactly these 30 frames only
- do not skip frames
- do not duplicate frames
- do not invent extra walk frames
- do not rename any frame
- labels must exactly match the frame names above
- keep all cells evenly sized
- keep the sprite centered above the label in each cell
- keep the board as a rigid contact-sheet grid
- do not turn the board into a poster or model sheet

Style:
- pixel art
- crisp edges
- slightly moody, cute, vulnerable desktop-pet tone
- upright body, bushy tail essential, nimble and alert
- male should read slightly larger and bolder
- indigo should be the dominant palette family without losing squirrel readability

Animation intent:
- idle: subtle breathing or blinking
- walk: readable side-view walk cycle
- eat: nibble or drink motion
- happy: excited positive motion
- sad: slumped low-energy pose
- sleep: clearly asleep and distinct from idle
- sick: visibly unwell and distinct from sad
- bathe: self-cleaning or washing motion

Return only the generated image.
```

## 102. squirrel / male / violet

```text
Generate an IMAGE ONLY.

Do not answer with text.
Do not describe limitations.
Do not provide configuration data.
Do not provide JSON.
Do not provide a table.
Do not provide a written explanation.

Create exactly one labeled sprite reference board image for this single variant only:

squirrel / male / violet

This prompt is self-contained. Do not rely on previous images.

Board rules:
- one single board only
- exactly 5 columns and exactly 6 rows
- exactly 30 total cells
- every row must contain exactly 5 cells
- every column must contain exactly 6 cells
- all cells must be equal size
- no merged cells
- no missing cells
- no extra cells
- no irregular row widths
- one sprite per cell
- one frame label at the bottom of each cell
- visible border lines between cells
- no title banner
- no extra decorations
- no extra animals
- no extra variants

Exact frame order:
Row 1: idle_00, idle_01, idle_02, idle_03, walk_00
Row 2: walk_01, walk_02, walk_03, walk_04, walk_05
Row 3: eat_00, eat_01, eat_02, eat_03, happy_00
Row 4: happy_01, happy_02, happy_03, sad_00, sad_01
Row 5: sleep_00, sleep_01, sick_00, sick_01, sick_02
Row 6: sick_03, bathe_00, bathe_01, bathe_02, bathe_03

Critical rules:
- exactly these 30 frames only
- do not skip frames
- do not duplicate frames
- do not invent extra walk frames
- do not rename any frame
- labels must exactly match the frame names above
- keep all cells evenly sized
- keep the sprite centered above the label in each cell
- keep the board as a rigid contact-sheet grid
- do not turn the board into a poster or model sheet

Style:
- pixel art
- crisp edges
- slightly moody, cute, vulnerable desktop-pet tone
- upright body, bushy tail essential, nimble and alert
- male should read slightly larger and bolder
- violet should be the dominant palette family without losing squirrel readability

Animation intent:
- idle: subtle breathing or blinking
- walk: readable side-view walk cycle
- eat: nibble or drink motion
- happy: excited positive motion
- sad: slumped low-energy pose
- sleep: clearly asleep and distinct from idle
- sick: visibly unwell and distinct from sad
- bathe: self-cleaning or washing motion

Return only the generated image.
```

## 103. squirrel / female / red

```text
Generate an IMAGE ONLY.

Do not answer with text.
Do not describe limitations.
Do not provide configuration data.
Do not provide JSON.
Do not provide a table.
Do not provide a written explanation.

Create exactly one labeled sprite reference board image for this single variant only:

squirrel / female / red

This prompt is self-contained. Do not rely on previous images.

Board rules:
- one single board only
- exactly 5 columns and exactly 6 rows
- exactly 30 total cells
- every row must contain exactly 5 cells
- every column must contain exactly 6 cells
- all cells must be equal size
- no merged cells
- no missing cells
- no extra cells
- no irregular row widths
- one sprite per cell
- one frame label at the bottom of each cell
- visible border lines between cells
- no title banner
- no extra decorations
- no extra animals
- no extra variants

Exact frame order:
Row 1: idle_00, idle_01, idle_02, idle_03, walk_00
Row 2: walk_01, walk_02, walk_03, walk_04, walk_05
Row 3: eat_00, eat_01, eat_02, eat_03, happy_00
Row 4: happy_01, happy_02, happy_03, sad_00, sad_01
Row 5: sleep_00, sleep_01, sick_00, sick_01, sick_02
Row 6: sick_03, bathe_00, bathe_01, bathe_02, bathe_03

Critical rules:
- exactly these 30 frames only
- do not skip frames
- do not duplicate frames
- do not invent extra walk frames
- do not rename any frame
- labels must exactly match the frame names above
- keep all cells evenly sized
- keep the sprite centered above the label in each cell
- keep the board as a rigid contact-sheet grid
- do not turn the board into a poster or model sheet

Style:
- pixel art
- crisp edges
- slightly moody, cute, vulnerable desktop-pet tone
- upright body, bushy tail essential, nimble and alert
- female should read slightly smaller and calmer
- red should be the dominant palette family without losing squirrel readability

Animation intent:
- idle: subtle breathing or blinking
- walk: readable side-view walk cycle
- eat: nibble or drink motion
- happy: excited positive motion
- sad: slumped low-energy pose
- sleep: clearly asleep and distinct from idle
- sick: visibly unwell and distinct from sad
- bathe: self-cleaning or washing motion

Return only the generated image.
```

## 104. squirrel / female / orange

```text
Generate an IMAGE ONLY.

Do not answer with text.
Do not describe limitations.
Do not provide configuration data.
Do not provide JSON.
Do not provide a table.
Do not provide a written explanation.

Create exactly one labeled sprite reference board image for this single variant only:

squirrel / female / orange

This prompt is self-contained. Do not rely on previous images.

Board rules:
- one single board only
- exactly 5 columns and exactly 6 rows
- exactly 30 total cells
- every row must contain exactly 5 cells
- every column must contain exactly 6 cells
- all cells must be equal size
- no merged cells
- no missing cells
- no extra cells
- no irregular row widths
- one sprite per cell
- one frame label at the bottom of each cell
- visible border lines between cells
- no title banner
- no extra decorations
- no extra animals
- no extra variants

Exact frame order:
Row 1: idle_00, idle_01, idle_02, idle_03, walk_00
Row 2: walk_01, walk_02, walk_03, walk_04, walk_05
Row 3: eat_00, eat_01, eat_02, eat_03, happy_00
Row 4: happy_01, happy_02, happy_03, sad_00, sad_01
Row 5: sleep_00, sleep_01, sick_00, sick_01, sick_02
Row 6: sick_03, bathe_00, bathe_01, bathe_02, bathe_03

Critical rules:
- exactly these 30 frames only
- do not skip frames
- do not duplicate frames
- do not invent extra walk frames
- do not rename any frame
- labels must exactly match the frame names above
- keep all cells evenly sized
- keep the sprite centered above the label in each cell
- keep the board as a rigid contact-sheet grid
- do not turn the board into a poster or model sheet

Style:
- pixel art
- crisp edges
- slightly moody, cute, vulnerable desktop-pet tone
- upright body, bushy tail essential, nimble and alert
- female should read slightly smaller and calmer
- orange should be the dominant palette family without losing squirrel readability

Animation intent:
- idle: subtle breathing or blinking
- walk: readable side-view walk cycle
- eat: nibble or drink motion
- happy: excited positive motion
- sad: slumped low-energy pose
- sleep: clearly asleep and distinct from idle
- sick: visibly unwell and distinct from sad
- bathe: self-cleaning or washing motion

Return only the generated image.
```

## 105. squirrel / female / yellow

```text
Generate an IMAGE ONLY.

Do not answer with text.
Do not describe limitations.
Do not provide configuration data.
Do not provide JSON.
Do not provide a table.
Do not provide a written explanation.

Create exactly one labeled sprite reference board image for this single variant only:

squirrel / female / yellow

This prompt is self-contained. Do not rely on previous images.

Board rules:
- one single board only
- exactly 5 columns and exactly 6 rows
- exactly 30 total cells
- every row must contain exactly 5 cells
- every column must contain exactly 6 cells
- all cells must be equal size
- no merged cells
- no missing cells
- no extra cells
- no irregular row widths
- one sprite per cell
- one frame label at the bottom of each cell
- visible border lines between cells
- no title banner
- no extra decorations
- no extra animals
- no extra variants

Exact frame order:
Row 1: idle_00, idle_01, idle_02, idle_03, walk_00
Row 2: walk_01, walk_02, walk_03, walk_04, walk_05
Row 3: eat_00, eat_01, eat_02, eat_03, happy_00
Row 4: happy_01, happy_02, happy_03, sad_00, sad_01
Row 5: sleep_00, sleep_01, sick_00, sick_01, sick_02
Row 6: sick_03, bathe_00, bathe_01, bathe_02, bathe_03

Critical rules:
- exactly these 30 frames only
- do not skip frames
- do not duplicate frames
- do not invent extra walk frames
- do not rename any frame
- labels must exactly match the frame names above
- keep all cells evenly sized
- keep the sprite centered above the label in each cell
- keep the board as a rigid contact-sheet grid
- do not turn the board into a poster or model sheet

Style:
- pixel art
- crisp edges
- slightly moody, cute, vulnerable desktop-pet tone
- upright body, bushy tail essential, nimble and alert
- female should read slightly smaller and calmer
- yellow should be the dominant palette family without losing squirrel readability

Animation intent:
- idle: subtle breathing or blinking
- walk: readable side-view walk cycle
- eat: nibble or drink motion
- happy: excited positive motion
- sad: slumped low-energy pose
- sleep: clearly asleep and distinct from idle
- sick: visibly unwell and distinct from sad
- bathe: self-cleaning or washing motion

Return only the generated image.
```

## 106. squirrel / female / blue

```text
Generate an IMAGE ONLY.

Do not answer with text.
Do not describe limitations.
Do not provide configuration data.
Do not provide JSON.
Do not provide a table.
Do not provide a written explanation.

Create exactly one labeled sprite reference board image for this single variant only:

squirrel / female / blue

This prompt is self-contained. Do not rely on previous images.

Board rules:
- one single board only
- exactly 5 columns and exactly 6 rows
- exactly 30 total cells
- every row must contain exactly 5 cells
- every column must contain exactly 6 cells
- all cells must be equal size
- no merged cells
- no missing cells
- no extra cells
- no irregular row widths
- one sprite per cell
- one frame label at the bottom of each cell
- visible border lines between cells
- no title banner
- no extra decorations
- no extra animals
- no extra variants

Exact frame order:
Row 1: idle_00, idle_01, idle_02, idle_03, walk_00
Row 2: walk_01, walk_02, walk_03, walk_04, walk_05
Row 3: eat_00, eat_01, eat_02, eat_03, happy_00
Row 4: happy_01, happy_02, happy_03, sad_00, sad_01
Row 5: sleep_00, sleep_01, sick_00, sick_01, sick_02
Row 6: sick_03, bathe_00, bathe_01, bathe_02, bathe_03

Critical rules:
- exactly these 30 frames only
- do not skip frames
- do not duplicate frames
- do not invent extra walk frames
- do not rename any frame
- labels must exactly match the frame names above
- keep all cells evenly sized
- keep the sprite centered above the label in each cell
- keep the board as a rigid contact-sheet grid
- do not turn the board into a poster or model sheet

Style:
- pixel art
- crisp edges
- slightly moody, cute, vulnerable desktop-pet tone
- upright body, bushy tail essential, nimble and alert
- female should read slightly smaller and calmer
- blue should be the dominant palette family without losing squirrel readability

Animation intent:
- idle: subtle breathing or blinking
- walk: readable side-view walk cycle
- eat: nibble or drink motion
- happy: excited positive motion
- sad: slumped low-energy pose
- sleep: clearly asleep and distinct from idle
- sick: visibly unwell and distinct from sad
- bathe: self-cleaning or washing motion

Return only the generated image.
```

## 107. squirrel / female / indigo

```text
Generate an IMAGE ONLY.

Do not answer with text.
Do not describe limitations.
Do not provide configuration data.
Do not provide JSON.
Do not provide a table.
Do not provide a written explanation.

Create exactly one labeled sprite reference board image for this single variant only:

squirrel / female / indigo

This prompt is self-contained. Do not rely on previous images.

Board rules:
- one single board only
- exactly 5 columns and exactly 6 rows
- exactly 30 total cells
- every row must contain exactly 5 cells
- every column must contain exactly 6 cells
- all cells must be equal size
- no merged cells
- no missing cells
- no extra cells
- no irregular row widths
- one sprite per cell
- one frame label at the bottom of each cell
- visible border lines between cells
- no title banner
- no extra decorations
- no extra animals
- no extra variants

Exact frame order:
Row 1: idle_00, idle_01, idle_02, idle_03, walk_00
Row 2: walk_01, walk_02, walk_03, walk_04, walk_05
Row 3: eat_00, eat_01, eat_02, eat_03, happy_00
Row 4: happy_01, happy_02, happy_03, sad_00, sad_01
Row 5: sleep_00, sleep_01, sick_00, sick_01, sick_02
Row 6: sick_03, bathe_00, bathe_01, bathe_02, bathe_03

Critical rules:
- exactly these 30 frames only
- do not skip frames
- do not duplicate frames
- do not invent extra walk frames
- do not rename any frame
- labels must exactly match the frame names above
- keep all cells evenly sized
- keep the sprite centered above the label in each cell
- keep the board as a rigid contact-sheet grid
- do not turn the board into a poster or model sheet

Style:
- pixel art
- crisp edges
- slightly moody, cute, vulnerable desktop-pet tone
- upright body, bushy tail essential, nimble and alert
- female should read slightly smaller and calmer
- indigo should be the dominant palette family without losing squirrel readability

Animation intent:
- idle: subtle breathing or blinking
- walk: readable side-view walk cycle
- eat: nibble or drink motion
- happy: excited positive motion
- sad: slumped low-energy pose
- sleep: clearly asleep and distinct from idle
- sick: visibly unwell and distinct from sad
- bathe: self-cleaning or washing motion

Return only the generated image.
```

## 108. squirrel / female / violet

```text
Generate an IMAGE ONLY.

Do not answer with text.
Do not describe limitations.
Do not provide configuration data.
Do not provide JSON.
Do not provide a table.
Do not provide a written explanation.

Create exactly one labeled sprite reference board image for this single variant only:

squirrel / female / violet

This prompt is self-contained. Do not rely on previous images.

Board rules:
- one single board only
- exactly 5 columns and exactly 6 rows
- exactly 30 total cells
- every row must contain exactly 5 cells
- every column must contain exactly 6 cells
- all cells must be equal size
- no merged cells
- no missing cells
- no extra cells
- no irregular row widths
- one sprite per cell
- one frame label at the bottom of each cell
- visible border lines between cells
- no title banner
- no extra decorations
- no extra animals
- no extra variants

Exact frame order:
Row 1: idle_00, idle_01, idle_02, idle_03, walk_00
Row 2: walk_01, walk_02, walk_03, walk_04, walk_05
Row 3: eat_00, eat_01, eat_02, eat_03, happy_00
Row 4: happy_01, happy_02, happy_03, sad_00, sad_01
Row 5: sleep_00, sleep_01, sick_00, sick_01, sick_02
Row 6: sick_03, bathe_00, bathe_01, bathe_02, bathe_03

Critical rules:
- exactly these 30 frames only
- do not skip frames
- do not duplicate frames
- do not invent extra walk frames
- do not rename any frame
- labels must exactly match the frame names above
- keep all cells evenly sized
- keep the sprite centered above the label in each cell
- keep the board as a rigid contact-sheet grid
- do not turn the board into a poster or model sheet

Style:
- pixel art
- crisp edges
- slightly moody, cute, vulnerable desktop-pet tone
- upright body, bushy tail essential, nimble and alert
- female should read slightly smaller and calmer
- violet should be the dominant palette family without losing squirrel readability

Animation intent:
- idle: subtle breathing or blinking
- walk: readable side-view walk cycle
- eat: nibble or drink motion
- happy: excited positive motion
- sad: slumped low-energy pose
- sleep: clearly asleep and distinct from idle
- sick: visibly unwell and distinct from sad
- bathe: self-cleaning or washing motion

Return only the generated image.
```

## 109. goose / male / red

```text
Generate an IMAGE ONLY.

Do not answer with text.
Do not describe limitations.
Do not provide configuration data.
Do not provide JSON.
Do not provide a table.
Do not provide a written explanation.

Create exactly one labeled sprite reference board image for this single variant only:

goose / male / red

This prompt is self-contained. Do not rely on previous images.

Board rules:
- one single board only
- exactly 5 columns and exactly 6 rows
- exactly 30 total cells
- every row must contain exactly 5 cells
- every column must contain exactly 6 cells
- all cells must be equal size
- no merged cells
- no missing cells
- no extra cells
- no irregular row widths
- one sprite per cell
- one frame label at the bottom of each cell
- visible border lines between cells
- no title banner
- no extra decorations
- no extra animals
- no extra variants

Exact frame order:
Row 1: idle_00, idle_01, idle_02, idle_03, walk_00
Row 2: walk_01, walk_02, walk_03, walk_04, walk_05
Row 3: eat_00, eat_01, eat_02, eat_03, happy_00
Row 4: happy_01, happy_02, happy_03, sad_00, sad_01
Row 5: sleep_00, sleep_01, sick_00, sick_01, sick_02
Row 6: sick_03, bathe_00, bathe_01, bathe_02, bathe_03

Critical rules:
- exactly these 30 frames only
- do not skip frames
- do not duplicate frames
- do not invent extra walk frames
- do not rename any frame
- labels must exactly match the frame names above
- keep all cells evenly sized
- keep the sprite centered above the label in each cell
- keep the board as a rigid contact-sheet grid
- do not turn the board into a poster or model sheet

Style:
- pixel art
- crisp edges
- slightly moody, cute, vulnerable desktop-pet tone
- long neck, larger bird body, heavier and more assertive than pigeon
- male should read slightly larger and bolder
- red should be the dominant palette family without losing goose readability

Animation intent:
- idle: subtle breathing or blinking
- walk: readable side-view walk cycle
- eat: nibble or drink motion
- happy: excited positive motion
- sad: slumped low-energy pose
- sleep: clearly asleep and distinct from idle
- sick: visibly unwell and distinct from sad
- bathe: self-cleaning or washing motion

Return only the generated image.
```

## 110. goose / male / orange

```text
Generate an IMAGE ONLY.

Do not answer with text.
Do not describe limitations.
Do not provide configuration data.
Do not provide JSON.
Do not provide a table.
Do not provide a written explanation.

Create exactly one labeled sprite reference board image for this single variant only:

goose / male / orange

This prompt is self-contained. Do not rely on previous images.

Board rules:
- one single board only
- exactly 5 columns and exactly 6 rows
- exactly 30 total cells
- every row must contain exactly 5 cells
- every column must contain exactly 6 cells
- all cells must be equal size
- no merged cells
- no missing cells
- no extra cells
- no irregular row widths
- one sprite per cell
- one frame label at the bottom of each cell
- visible border lines between cells
- no title banner
- no extra decorations
- no extra animals
- no extra variants

Exact frame order:
Row 1: idle_00, idle_01, idle_02, idle_03, walk_00
Row 2: walk_01, walk_02, walk_03, walk_04, walk_05
Row 3: eat_00, eat_01, eat_02, eat_03, happy_00
Row 4: happy_01, happy_02, happy_03, sad_00, sad_01
Row 5: sleep_00, sleep_01, sick_00, sick_01, sick_02
Row 6: sick_03, bathe_00, bathe_01, bathe_02, bathe_03

Critical rules:
- exactly these 30 frames only
- do not skip frames
- do not duplicate frames
- do not invent extra walk frames
- do not rename any frame
- labels must exactly match the frame names above
- keep all cells evenly sized
- keep the sprite centered above the label in each cell
- keep the board as a rigid contact-sheet grid
- do not turn the board into a poster or model sheet

Style:
- pixel art
- crisp edges
- slightly moody, cute, vulnerable desktop-pet tone
- long neck, larger bird body, heavier and more assertive than pigeon
- male should read slightly larger and bolder
- orange should be the dominant palette family without losing goose readability

Animation intent:
- idle: subtle breathing or blinking
- walk: readable side-view walk cycle
- eat: nibble or drink motion
- happy: excited positive motion
- sad: slumped low-energy pose
- sleep: clearly asleep and distinct from idle
- sick: visibly unwell and distinct from sad
- bathe: self-cleaning or washing motion

Return only the generated image.
```

## 111. goose / male / yellow

```text
Generate an IMAGE ONLY.

Do not answer with text.
Do not describe limitations.
Do not provide configuration data.
Do not provide JSON.
Do not provide a table.
Do not provide a written explanation.

Create exactly one labeled sprite reference board image for this single variant only:

goose / male / yellow

This prompt is self-contained. Do not rely on previous images.

Board rules:
- one single board only
- exactly 5 columns and exactly 6 rows
- exactly 30 total cells
- every row must contain exactly 5 cells
- every column must contain exactly 6 cells
- all cells must be equal size
- no merged cells
- no missing cells
- no extra cells
- no irregular row widths
- one sprite per cell
- one frame label at the bottom of each cell
- visible border lines between cells
- no title banner
- no extra decorations
- no extra animals
- no extra variants

Exact frame order:
Row 1: idle_00, idle_01, idle_02, idle_03, walk_00
Row 2: walk_01, walk_02, walk_03, walk_04, walk_05
Row 3: eat_00, eat_01, eat_02, eat_03, happy_00
Row 4: happy_01, happy_02, happy_03, sad_00, sad_01
Row 5: sleep_00, sleep_01, sick_00, sick_01, sick_02
Row 6: sick_03, bathe_00, bathe_01, bathe_02, bathe_03

Critical rules:
- exactly these 30 frames only
- do not skip frames
- do not duplicate frames
- do not invent extra walk frames
- do not rename any frame
- labels must exactly match the frame names above
- keep all cells evenly sized
- keep the sprite centered above the label in each cell
- keep the board as a rigid contact-sheet grid
- do not turn the board into a poster or model sheet

Style:
- pixel art
- crisp edges
- slightly moody, cute, vulnerable desktop-pet tone
- long neck, larger bird body, heavier and more assertive than pigeon
- male should read slightly larger and bolder
- yellow should be the dominant palette family without losing goose readability

Animation intent:
- idle: subtle breathing or blinking
- walk: readable side-view walk cycle
- eat: nibble or drink motion
- happy: excited positive motion
- sad: slumped low-energy pose
- sleep: clearly asleep and distinct from idle
- sick: visibly unwell and distinct from sad
- bathe: self-cleaning or washing motion

Return only the generated image.
```

## 112. goose / male / blue

```text
Generate an IMAGE ONLY.

Do not answer with text.
Do not describe limitations.
Do not provide configuration data.
Do not provide JSON.
Do not provide a table.
Do not provide a written explanation.

Create exactly one labeled sprite reference board image for this single variant only:

goose / male / blue

This prompt is self-contained. Do not rely on previous images.

Board rules:
- one single board only
- exactly 5 columns and exactly 6 rows
- exactly 30 total cells
- every row must contain exactly 5 cells
- every column must contain exactly 6 cells
- all cells must be equal size
- no merged cells
- no missing cells
- no extra cells
- no irregular row widths
- one sprite per cell
- one frame label at the bottom of each cell
- visible border lines between cells
- no title banner
- no extra decorations
- no extra animals
- no extra variants

Exact frame order:
Row 1: idle_00, idle_01, idle_02, idle_03, walk_00
Row 2: walk_01, walk_02, walk_03, walk_04, walk_05
Row 3: eat_00, eat_01, eat_02, eat_03, happy_00
Row 4: happy_01, happy_02, happy_03, sad_00, sad_01
Row 5: sleep_00, sleep_01, sick_00, sick_01, sick_02
Row 6: sick_03, bathe_00, bathe_01, bathe_02, bathe_03

Critical rules:
- exactly these 30 frames only
- do not skip frames
- do not duplicate frames
- do not invent extra walk frames
- do not rename any frame
- labels must exactly match the frame names above
- keep all cells evenly sized
- keep the sprite centered above the label in each cell
- keep the board as a rigid contact-sheet grid
- do not turn the board into a poster or model sheet

Style:
- pixel art
- crisp edges
- slightly moody, cute, vulnerable desktop-pet tone
- long neck, larger bird body, heavier and more assertive than pigeon
- male should read slightly larger and bolder
- blue should be the dominant palette family without losing goose readability

Animation intent:
- idle: subtle breathing or blinking
- walk: readable side-view walk cycle
- eat: nibble or drink motion
- happy: excited positive motion
- sad: slumped low-energy pose
- sleep: clearly asleep and distinct from idle
- sick: visibly unwell and distinct from sad
- bathe: self-cleaning or washing motion

Return only the generated image.
```

## 113. goose / male / indigo

```text
Generate an IMAGE ONLY.

Do not answer with text.
Do not describe limitations.
Do not provide configuration data.
Do not provide JSON.
Do not provide a table.
Do not provide a written explanation.

Create exactly one labeled sprite reference board image for this single variant only:

goose / male / indigo

This prompt is self-contained. Do not rely on previous images.

Board rules:
- one single board only
- exactly 5 columns and exactly 6 rows
- exactly 30 total cells
- every row must contain exactly 5 cells
- every column must contain exactly 6 cells
- all cells must be equal size
- no merged cells
- no missing cells
- no extra cells
- no irregular row widths
- one sprite per cell
- one frame label at the bottom of each cell
- visible border lines between cells
- no title banner
- no extra decorations
- no extra animals
- no extra variants

Exact frame order:
Row 1: idle_00, idle_01, idle_02, idle_03, walk_00
Row 2: walk_01, walk_02, walk_03, walk_04, walk_05
Row 3: eat_00, eat_01, eat_02, eat_03, happy_00
Row 4: happy_01, happy_02, happy_03, sad_00, sad_01
Row 5: sleep_00, sleep_01, sick_00, sick_01, sick_02
Row 6: sick_03, bathe_00, bathe_01, bathe_02, bathe_03

Critical rules:
- exactly these 30 frames only
- do not skip frames
- do not duplicate frames
- do not invent extra walk frames
- do not rename any frame
- labels must exactly match the frame names above
- keep all cells evenly sized
- keep the sprite centered above the label in each cell
- keep the board as a rigid contact-sheet grid
- do not turn the board into a poster or model sheet

Style:
- pixel art
- crisp edges
- slightly moody, cute, vulnerable desktop-pet tone
- long neck, larger bird body, heavier and more assertive than pigeon
- male should read slightly larger and bolder
- indigo should be the dominant palette family without losing goose readability

Animation intent:
- idle: subtle breathing or blinking
- walk: readable side-view walk cycle
- eat: nibble or drink motion
- happy: excited positive motion
- sad: slumped low-energy pose
- sleep: clearly asleep and distinct from idle
- sick: visibly unwell and distinct from sad
- bathe: self-cleaning or washing motion

Return only the generated image.
```

## 114. goose / male / violet

```text
Generate an IMAGE ONLY.

Do not answer with text.
Do not describe limitations.
Do not provide configuration data.
Do not provide JSON.
Do not provide a table.
Do not provide a written explanation.

Create exactly one labeled sprite reference board image for this single variant only:

goose / male / violet

This prompt is self-contained. Do not rely on previous images.

Board rules:
- one single board only
- exactly 5 columns and exactly 6 rows
- exactly 30 total cells
- every row must contain exactly 5 cells
- every column must contain exactly 6 cells
- all cells must be equal size
- no merged cells
- no missing cells
- no extra cells
- no irregular row widths
- one sprite per cell
- one frame label at the bottom of each cell
- visible border lines between cells
- no title banner
- no extra decorations
- no extra animals
- no extra variants

Exact frame order:
Row 1: idle_00, idle_01, idle_02, idle_03, walk_00
Row 2: walk_01, walk_02, walk_03, walk_04, walk_05
Row 3: eat_00, eat_01, eat_02, eat_03, happy_00
Row 4: happy_01, happy_02, happy_03, sad_00, sad_01
Row 5: sleep_00, sleep_01, sick_00, sick_01, sick_02
Row 6: sick_03, bathe_00, bathe_01, bathe_02, bathe_03

Critical rules:
- exactly these 30 frames only
- do not skip frames
- do not duplicate frames
- do not invent extra walk frames
- do not rename any frame
- labels must exactly match the frame names above
- keep all cells evenly sized
- keep the sprite centered above the label in each cell
- keep the board as a rigid contact-sheet grid
- do not turn the board into a poster or model sheet

Style:
- pixel art
- crisp edges
- slightly moody, cute, vulnerable desktop-pet tone
- long neck, larger bird body, heavier and more assertive than pigeon
- male should read slightly larger and bolder
- violet should be the dominant palette family without losing goose readability

Animation intent:
- idle: subtle breathing or blinking
- walk: readable side-view walk cycle
- eat: nibble or drink motion
- happy: excited positive motion
- sad: slumped low-energy pose
- sleep: clearly asleep and distinct from idle
- sick: visibly unwell and distinct from sad
- bathe: self-cleaning or washing motion

Return only the generated image.
```

## 115. goose / female / red

```text
Generate an IMAGE ONLY.

Do not answer with text.
Do not describe limitations.
Do not provide configuration data.
Do not provide JSON.
Do not provide a table.
Do not provide a written explanation.

Create exactly one labeled sprite reference board image for this single variant only:

goose / female / red

This prompt is self-contained. Do not rely on previous images.

Board rules:
- one single board only
- exactly 5 columns and exactly 6 rows
- exactly 30 total cells
- every row must contain exactly 5 cells
- every column must contain exactly 6 cells
- all cells must be equal size
- no merged cells
- no missing cells
- no extra cells
- no irregular row widths
- one sprite per cell
- one frame label at the bottom of each cell
- visible border lines between cells
- no title banner
- no extra decorations
- no extra animals
- no extra variants

Exact frame order:
Row 1: idle_00, idle_01, idle_02, idle_03, walk_00
Row 2: walk_01, walk_02, walk_03, walk_04, walk_05
Row 3: eat_00, eat_01, eat_02, eat_03, happy_00
Row 4: happy_01, happy_02, happy_03, sad_00, sad_01
Row 5: sleep_00, sleep_01, sick_00, sick_01, sick_02
Row 6: sick_03, bathe_00, bathe_01, bathe_02, bathe_03

Critical rules:
- exactly these 30 frames only
- do not skip frames
- do not duplicate frames
- do not invent extra walk frames
- do not rename any frame
- labels must exactly match the frame names above
- keep all cells evenly sized
- keep the sprite centered above the label in each cell
- keep the board as a rigid contact-sheet grid
- do not turn the board into a poster or model sheet

Style:
- pixel art
- crisp edges
- slightly moody, cute, vulnerable desktop-pet tone
- long neck, larger bird body, heavier and more assertive than pigeon
- female should read slightly smaller and calmer
- red should be the dominant palette family without losing goose readability

Animation intent:
- idle: subtle breathing or blinking
- walk: readable side-view walk cycle
- eat: nibble or drink motion
- happy: excited positive motion
- sad: slumped low-energy pose
- sleep: clearly asleep and distinct from idle
- sick: visibly unwell and distinct from sad
- bathe: self-cleaning or washing motion

Return only the generated image.
```

## 116. goose / female / orange

```text
Generate an IMAGE ONLY.

Do not answer with text.
Do not describe limitations.
Do not provide configuration data.
Do not provide JSON.
Do not provide a table.
Do not provide a written explanation.

Create exactly one labeled sprite reference board image for this single variant only:

goose / female / orange

This prompt is self-contained. Do not rely on previous images.

Board rules:
- one single board only
- exactly 5 columns and exactly 6 rows
- exactly 30 total cells
- every row must contain exactly 5 cells
- every column must contain exactly 6 cells
- all cells must be equal size
- no merged cells
- no missing cells
- no extra cells
- no irregular row widths
- one sprite per cell
- one frame label at the bottom of each cell
- visible border lines between cells
- no title banner
- no extra decorations
- no extra animals
- no extra variants

Exact frame order:
Row 1: idle_00, idle_01, idle_02, idle_03, walk_00
Row 2: walk_01, walk_02, walk_03, walk_04, walk_05
Row 3: eat_00, eat_01, eat_02, eat_03, happy_00
Row 4: happy_01, happy_02, happy_03, sad_00, sad_01
Row 5: sleep_00, sleep_01, sick_00, sick_01, sick_02
Row 6: sick_03, bathe_00, bathe_01, bathe_02, bathe_03

Critical rules:
- exactly these 30 frames only
- do not skip frames
- do not duplicate frames
- do not invent extra walk frames
- do not rename any frame
- labels must exactly match the frame names above
- keep all cells evenly sized
- keep the sprite centered above the label in each cell
- keep the board as a rigid contact-sheet grid
- do not turn the board into a poster or model sheet

Style:
- pixel art
- crisp edges
- slightly moody, cute, vulnerable desktop-pet tone
- long neck, larger bird body, heavier and more assertive than pigeon
- female should read slightly smaller and calmer
- orange should be the dominant palette family without losing goose readability

Animation intent:
- idle: subtle breathing or blinking
- walk: readable side-view walk cycle
- eat: nibble or drink motion
- happy: excited positive motion
- sad: slumped low-energy pose
- sleep: clearly asleep and distinct from idle
- sick: visibly unwell and distinct from sad
- bathe: self-cleaning or washing motion

Return only the generated image.
```

## 117. goose / female / yellow

```text
Generate an IMAGE ONLY.

Do not answer with text.
Do not describe limitations.
Do not provide configuration data.
Do not provide JSON.
Do not provide a table.
Do not provide a written explanation.

Create exactly one labeled sprite reference board image for this single variant only:

goose / female / yellow

This prompt is self-contained. Do not rely on previous images.

Board rules:
- one single board only
- exactly 5 columns and exactly 6 rows
- exactly 30 total cells
- every row must contain exactly 5 cells
- every column must contain exactly 6 cells
- all cells must be equal size
- no merged cells
- no missing cells
- no extra cells
- no irregular row widths
- one sprite per cell
- one frame label at the bottom of each cell
- visible border lines between cells
- no title banner
- no extra decorations
- no extra animals
- no extra variants

Exact frame order:
Row 1: idle_00, idle_01, idle_02, idle_03, walk_00
Row 2: walk_01, walk_02, walk_03, walk_04, walk_05
Row 3: eat_00, eat_01, eat_02, eat_03, happy_00
Row 4: happy_01, happy_02, happy_03, sad_00, sad_01
Row 5: sleep_00, sleep_01, sick_00, sick_01, sick_02
Row 6: sick_03, bathe_00, bathe_01, bathe_02, bathe_03

Critical rules:
- exactly these 30 frames only
- do not skip frames
- do not duplicate frames
- do not invent extra walk frames
- do not rename any frame
- labels must exactly match the frame names above
- keep all cells evenly sized
- keep the sprite centered above the label in each cell
- keep the board as a rigid contact-sheet grid
- do not turn the board into a poster or model sheet

Style:
- pixel art
- crisp edges
- slightly moody, cute, vulnerable desktop-pet tone
- long neck, larger bird body, heavier and more assertive than pigeon
- female should read slightly smaller and calmer
- yellow should be the dominant palette family without losing goose readability

Animation intent:
- idle: subtle breathing or blinking
- walk: readable side-view walk cycle
- eat: nibble or drink motion
- happy: excited positive motion
- sad: slumped low-energy pose
- sleep: clearly asleep and distinct from idle
- sick: visibly unwell and distinct from sad
- bathe: self-cleaning or washing motion

Return only the generated image.
```

## 118. goose / female / blue

```text
Generate an IMAGE ONLY.

Do not answer with text.
Do not describe limitations.
Do not provide configuration data.
Do not provide JSON.
Do not provide a table.
Do not provide a written explanation.

Create exactly one labeled sprite reference board image for this single variant only:

goose / female / blue

This prompt is self-contained. Do not rely on previous images.

Board rules:
- one single board only
- exactly 5 columns and exactly 6 rows
- exactly 30 total cells
- every row must contain exactly 5 cells
- every column must contain exactly 6 cells
- all cells must be equal size
- no merged cells
- no missing cells
- no extra cells
- no irregular row widths
- one sprite per cell
- one frame label at the bottom of each cell
- visible border lines between cells
- no title banner
- no extra decorations
- no extra animals
- no extra variants

Exact frame order:
Row 1: idle_00, idle_01, idle_02, idle_03, walk_00
Row 2: walk_01, walk_02, walk_03, walk_04, walk_05
Row 3: eat_00, eat_01, eat_02, eat_03, happy_00
Row 4: happy_01, happy_02, happy_03, sad_00, sad_01
Row 5: sleep_00, sleep_01, sick_00, sick_01, sick_02
Row 6: sick_03, bathe_00, bathe_01, bathe_02, bathe_03

Critical rules:
- exactly these 30 frames only
- do not skip frames
- do not duplicate frames
- do not invent extra walk frames
- do not rename any frame
- labels must exactly match the frame names above
- keep all cells evenly sized
- keep the sprite centered above the label in each cell
- keep the board as a rigid contact-sheet grid
- do not turn the board into a poster or model sheet

Style:
- pixel art
- crisp edges
- slightly moody, cute, vulnerable desktop-pet tone
- long neck, larger bird body, heavier and more assertive than pigeon
- female should read slightly smaller and calmer
- blue should be the dominant palette family without losing goose readability

Animation intent:
- idle: subtle breathing or blinking
- walk: readable side-view walk cycle
- eat: nibble or drink motion
- happy: excited positive motion
- sad: slumped low-energy pose
- sleep: clearly asleep and distinct from idle
- sick: visibly unwell and distinct from sad
- bathe: self-cleaning or washing motion

Return only the generated image.
```

## 119. goose / female / indigo

```text
Generate an IMAGE ONLY.

Do not answer with text.
Do not describe limitations.
Do not provide configuration data.
Do not provide JSON.
Do not provide a table.
Do not provide a written explanation.

Create exactly one labeled sprite reference board image for this single variant only:

goose / female / indigo

This prompt is self-contained. Do not rely on previous images.

Board rules:
- one single board only
- exactly 5 columns and exactly 6 rows
- exactly 30 total cells
- every row must contain exactly 5 cells
- every column must contain exactly 6 cells
- all cells must be equal size
- no merged cells
- no missing cells
- no extra cells
- no irregular row widths
- one sprite per cell
- one frame label at the bottom of each cell
- visible border lines between cells
- no title banner
- no extra decorations
- no extra animals
- no extra variants

Exact frame order:
Row 1: idle_00, idle_01, idle_02, idle_03, walk_00
Row 2: walk_01, walk_02, walk_03, walk_04, walk_05
Row 3: eat_00, eat_01, eat_02, eat_03, happy_00
Row 4: happy_01, happy_02, happy_03, sad_00, sad_01
Row 5: sleep_00, sleep_01, sick_00, sick_01, sick_02
Row 6: sick_03, bathe_00, bathe_01, bathe_02, bathe_03

Critical rules:
- exactly these 30 frames only
- do not skip frames
- do not duplicate frames
- do not invent extra walk frames
- do not rename any frame
- labels must exactly match the frame names above
- keep all cells evenly sized
- keep the sprite centered above the label in each cell
- keep the board as a rigid contact-sheet grid
- do not turn the board into a poster or model sheet

Style:
- pixel art
- crisp edges
- slightly moody, cute, vulnerable desktop-pet tone
- long neck, larger bird body, heavier and more assertive than pigeon
- female should read slightly smaller and calmer
- indigo should be the dominant palette family without losing goose readability

Animation intent:
- idle: subtle breathing or blinking
- walk: readable side-view walk cycle
- eat: nibble or drink motion
- happy: excited positive motion
- sad: slumped low-energy pose
- sleep: clearly asleep and distinct from idle
- sick: visibly unwell and distinct from sad
- bathe: self-cleaning or washing motion

Return only the generated image.
```

## 120. goose / female / violet

```text
Generate an IMAGE ONLY.

Do not answer with text.
Do not describe limitations.
Do not provide configuration data.
Do not provide JSON.
Do not provide a table.
Do not provide a written explanation.

Create exactly one labeled sprite reference board image for this single variant only:

goose / female / violet

This prompt is self-contained. Do not rely on previous images.

Board rules:
- one single board only
- exactly 5 columns and exactly 6 rows
- exactly 30 total cells
- every row must contain exactly 5 cells
- every column must contain exactly 6 cells
- all cells must be equal size
- no merged cells
- no missing cells
- no extra cells
- no irregular row widths
- one sprite per cell
- one frame label at the bottom of each cell
- visible border lines between cells
- no title banner
- no extra decorations
- no extra animals
- no extra variants

Exact frame order:
Row 1: idle_00, idle_01, idle_02, idle_03, walk_00
Row 2: walk_01, walk_02, walk_03, walk_04, walk_05
Row 3: eat_00, eat_01, eat_02, eat_03, happy_00
Row 4: happy_01, happy_02, happy_03, sad_00, sad_01
Row 5: sleep_00, sleep_01, sick_00, sick_01, sick_02
Row 6: sick_03, bathe_00, bathe_01, bathe_02, bathe_03

Critical rules:
- exactly these 30 frames only
- do not skip frames
- do not duplicate frames
- do not invent extra walk frames
- do not rename any frame
- labels must exactly match the frame names above
- keep all cells evenly sized
- keep the sprite centered above the label in each cell
- keep the board as a rigid contact-sheet grid
- do not turn the board into a poster or model sheet

Style:
- pixel art
- crisp edges
- slightly moody, cute, vulnerable desktop-pet tone
- long neck, larger bird body, heavier and more assertive than pigeon
- female should read slightly smaller and calmer
- violet should be the dominant palette family without losing goose readability

Animation intent:
- idle: subtle breathing or blinking
- walk: readable side-view walk cycle
- eat: nibble or drink motion
- happy: excited positive motion
- sad: slumped low-energy pose
- sleep: clearly asleep and distinct from idle
- sick: visibly unwell and distinct from sad
- bathe: self-cleaning or washing motion

Return only the generated image.
```
