   M S B A M L   `   `   `  ���� N  JRemnantWorldChanger, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null9!clr-namespace:RemnantWorldChangerRemnantWorldChanger  $    RemnantWorldChanger.MainWindow   W SPresentationCore, Version=6.0.2.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35\ XPresentationFramework, Version=6.0.2.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35e aSystem.Windows.Controls.Ribbon, Version=6.0.2.0, Culture=neutral, PublicKeyToken=b77a5c561934e089R NWindowsBase, Version=6.0.2.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35F 9http://schemas.microsoft.com/winfx/2006/xaml/presentation     R NSystem.Xaml, Version=6.0.2.0, Culture=neutral, PublicKeyToken=b77a5c561934e089:x,http://schemas.microsoft.com/winfx/2006/xaml     @d2http://schemas.microsoft.com/expression/blend/2008     Jmc;http://schemas.openxmlformats.org/markup-compatibility/2006     3local!clr-namespace:RemnantWorldChanger     -     � 
ResizeMode$  NoResize=� � Title$ Remnant World Changer��$	��450��$	��450��.��]� ���#220000.+�o� $	��Top=�$��20��$	��Top=�.@�m� $��Options��.@�m� $��Auto-Update�� m� IsCheckable$
 True��${�False��m� $��Check for update��${�False��m� -   $��Open Data Folder��m� -   $��btnGenExample��$��Generate Test Saves��$y�	Collapsed=�${�false���� -   $��
cbKeepSave��$��Keep Checkpoint��]� $	��Top=�$��20��$	��Top=�.+��� -   $��cmbSaveType��$
��Left=�$
��Left=�$	��100���� -   $��tbSearchbar��$	��230��$��10,0,0,0q��� -   $��Add Checkpoint��$	��Top=�$��Right=�$	��100��$��20��]� .+�&   System.Windows.Controls.DataGrid  -   $��SaveList��   SelectionMode$ Single=�$
��Left=�$	��200��$
��Left=�   AutoGenerateColumns$ False��   CanUserReorderColumns$ False��   CanUserResizeColumns$ False��   
IsReadOnly$
 True����Z�    Columns 0  *System.Windows.Controls.DataGridTextColumn  ,  &System.Windows.Controls.DataGridColumn	   Header$	 World��
   Width5  /System.Windows.Controls.DataGridLengthConverter$
 * 1  +System.Windows.Controls.DataGridBoundColumn   Binding �� *Value+  $
	 Name��$
 2*  �� *Key+ � ContextMenu �� .@�m� -	   $��Delete Save��m� -
   $��	Edit Save��m� -   $��	Load Save��]� $��Right=�.+��� -   $��DifficultyList�� �� SelectionMode$ Single=�$	��Top=�$	��110��$��Hidden=�$��Hidden=� �� View ��  �� AllowsColumnReorder$ False��.P���  �� Width$	 230��.���� $��
Difficulty��
���   �$��20�� �� .@�m� -   $��Delete Save��m� -   $��	Edit Save��m� -   $��	Load Save���� -   $��ModifierList��$ Single=�$��Bottom=�$��Hidden=� �� $ False��.P��� $	 230��.���� $��	Modifiers��
���   �$��20�� �� .@�m� -   $��Delete Save��m� -   $��	Edit Save��m� -   $��	Load Save��