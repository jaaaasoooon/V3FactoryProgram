   M S B A M L   `   `   `  ����D  @BoqiangH5, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null%clr-namespace:BoqiangH5	BoqiangH5  5   	       BoqiangH5.SettingMacWnd   5   	   \ XPresentationFramework, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35R NWindowsBase, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35W SPresentationCore, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35D 9http://schemas.microsoft.com/winfx/2006/xaml/presentation    5   	   R NSystem.Xaml, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089:x,http://schemas.microsoft.com/winfx/2006/xaml     5   	   @d2http://schemas.microsoft.com/expression/blend/2008     5   	   Jmc;http://schemas.openxmlformats.org/markup-compatibility/2006     5   	   )localclr-namespace:BoqiangH5     5   	   -   5          � Title$  	MAC设置��5   	   $	��450��6   $	��420��6$    � WindowStartupLocation$ CenterScreen=�60    � 
ResizeMode$ NoResize=�5	   :   .��5
      �  � RowDefinitions 5   
   �� 5      $��45 �6   6*   �� 5      $
��100* �6   6,   �� 5      $
��200* �6   6,   .+�5   
   � $��5q�6   $��0��6    � ColumnDefinitions 5      �� 5      $��80 �6#   60   �� 5      $	��80* �6#   61   �� 5      $��80 �6#   60   �� 5      $��80 �6#   60   .+�5      �� $��0��6   $��5q�6$   $��Mac地址：��6/   6B   �� 5      -   5        $��tbMac��5   1   $��1��6   $��5q�6&   $��Center=�6>   6b   �� 5      -   5        $��btnAdd��5   0   $��2��6   $��5q�6%   $��添加��6>   6b   �� 5      -   5        $��btnAlter��5   0   $��3��6   $��5q�6%   $��修改��6@   6f   5      �� 5   
   -   5        $��gbOperation��5   =   $��1��6   $��5 0 5 0q�6    $��操作��61   .��5      � -   5        $��gbGrid��5       5      �� 5      $��* �6'   63   �� 5       $��* �6'   63   �� 5!      $��* �6'   63   �� 5"      $��* �6'   63    5$      �� 5%      $��* �6$   61   �� 5&      $��* �6$   61   �� 5'      $��* �6$   61   .+�5)      �� -   5        $��chkRegister��5)   8   $��0��6   $��0��6(   $��	BMS注册��6K   $��Center=�6[   $
��Left=�6v   6�   �� 5*      -   5        $��	chkConfig��5*   8   $��0��6   $��1��6(   $��一键出厂配置��6I   $��Center=�6Z   $
��Left=�6u   6�   �� 5+      -	   5        $��	chkCharge��5+   8   $��0��6   $��2��6(   $��充放电测试��6I   $��Center=�6Y   $
��Left=�6t   6�   �� 5,      -
   5        $��chkCheck��5,   8   $��0��6   $��3��6(   $��一键出厂检验��6H   $��Center=�6Y   $
��Left=�6t   6�   �� 5-      -   5        $��chkClose��5-   8   $��1��6   $��0��6(   $��关机��6H   $��Center=�6U   $
��Left=�6p   6�   �� 5.      -   5        $��chkSleep��5.   8   $��1��6   $��1��6(   $��休眠��6H   $��Center=�6U   $
��Left=�6p   6�   �� 5/      -   5        $��
chkPowerOn��5/   8   $��1��6   $��2��6(   $��上电��6J   $��Center=�6W   $
��Left=�6r   6�   �� 50      -   5        $��chkPowerOff��50   8   $��1��6   $��3��6(   $��下电��6K   $��Center=�6X   $
��Left=�6s   6�   �� 51      -   5        $��chkSoc��51   8   $��2��6   $��0��6(   $��	SOC校准��6F   $��Center=�6V   $
��Left=�6q   6�   �� 52      -   5        $��chkZero��52   8   $��2��6   $��1��6(   $��零点校准��6G   $��Center=�6V   $
��Left=�6q   6�   �� 53      -   5        $��chk10A��53   8   $��2��6   $��2��6(   $��负10A校准��6F   $��Center=�6W   $
��Left=�6r   6�   �� 54      -   5        $��
chkBinding��54   8   $��2��6   $��3��6(   $��	BMS绑定��6J   $��Center=�6Z   $
��Left=�6u   6�   55      56      �� 57   
   
�������$��1q�6)   $��5q�6=   $��2��6H   ._�58      &   System.Windows.Controls.DataGrid  -   5        $��	dgMacInfo��58         CanUserAddRows .     CanUserSortColumns .     AutoGenerateColumns .  $��14(�6v   $
��0,10q�6�      RowHeaderWidth$ 0Q�6�   ��.  	   Columns	 59      0  *System.Windows.Controls.DataGridTextColumn  5:      ,  &System.Windows.Controls.DataGridColumn
   Header$
 	MAC地址��6)      DataGridHeaderStyle   HeaderStyle# [  69      Width5  /System.Windows.Controls.DataGridLengthConverter$	 150 6�      
IsReadOnly . 1  +System.Windows.Controls.DataGridBoundColumn   Binding 6l   ��  �� Path$	 Mac�   ElementStyle 5;      �� 5<       �� 
TargetType" ��6$   .�5=   "   ��  �� Property �@��TextAlignment �� Value$ Center=�6B   6S   �� 5>   "    � ��$ Center=�6F   6W   �� 5?   "    � ��$
 Wrap=�6A   6P   5@      5B        5C      $
 已选操作��6)   # [  67   $
 100* 6�    .  6j   �� $ 
Operations� 5D      �� 5E      " ��6$   .�5F   "   ��  �@��TextAlignment$ Center=�6B   6S   �� 5G   "    � ��$ Center=�6F   6W   �� 5H   "    � ��$
 Wrap=�6A   6P   5I      5K      4  .System.Windows.Controls.DataGridTemplateColumn  5L      $
 操作��6-   # [  69   $ 80 6l    .    CellTemplate 5M      �� 5N      .R�5O   "   � .+�5P   &   �� $��	btnDelete��6-   -   5        $��删除��5P   >   $��Center=�6K   $��Center=�6i   $��1��6�   6�   5Q   #   5R      5T      5V      5W      5X      5Y      