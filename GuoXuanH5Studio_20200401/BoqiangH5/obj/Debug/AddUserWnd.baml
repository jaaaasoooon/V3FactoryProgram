   M S B A M L   `   `   `  ����D  @BoqiangH5, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null%clr-namespace:BoqiangH5	BoqiangH5  5   	       BoqiangH5.AddUserWnd   5   	   \ XPresentationFramework, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35R NWindowsBase, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35W SPresentationCore, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35D 9http://schemas.microsoft.com/winfx/2006/xaml/presentation    5   	   R NSystem.Xaml, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089:x,http://schemas.microsoft.com/winfx/2006/xaml     5   	   @d2http://schemas.microsoft.com/expression/blend/2008     5   	   Jmc;http://schemas.openxmlformats.org/markup-compatibility/2006     5   	   )localclr-namespace:BoqiangH5     5   	   -   5          � Title$  
AddUserWnd��5   	   $	��500��6   $	��700��6)    � WindowStartupLocation$ CenterScreen=�65    � WindowStyle$
 None=�6Z   .��5	      �  � RowDefinitions 5
   
   �� 5      $��45 �6   6*   �� 5      $	��100 �6   6+   �� 5      $��* �6   6)   .+�5   
   �� $��5 0 5 5q�6   $��0��6"   
�������$��1q�6G   ._�5      �� -   5        $��btnClose��5      $��X��6%   $��1��61   $��4��6>   $��5q�6N   $��20��6Y   $��20��6e   $��Right=�6�   6�   5      �� 5   
   $��5q�6   $��1��6   
�������$��1q�6A   ._�5      �  5      �� 5      $��* �6$   61   �� 5      $��* �6$   61    � ColumnDefinitions 5      �� 5      $��60 �6'   64   �� 5      $	��80* �6'   65   �� 5      $��60 �6'   64   �� 5      $	��80* �6'   65   �� 5      $��80 �6'   64   .+�5      �� $��	工号：��6   $��Right=�6&   $��0��6B   $��0��6O   $��Center=�6_   6|   �� 5       -   5        $��tbUserNo��5       $��0��6*   $��1��67   $��30��6G   $��5q�6T   $��Center=�6_   6�   �� 5!      $��	姓名：��6   $��Right=�6&   $��0��6B   $��2��6O   $��Center=�6_   6|   �� 5"      -   5        $��
tbUserName��5"      $��0��6,   $��3��69   $��30��6I   $��5q�6V   $��Center=�6a   6�   �� 5#      $��	密码：��6   $��Right=�6&   $��1��6B   $��0��6O   $��Center=�6_   6|   �� 5$      -   5        $��
tbPassword��5$      $��1��6,   $��1��69   $��30��6I   $��5q�6V   $��Center=�6a   6�   �� 5%      $��	职务：��6   $��Right=�6&   $��1��6B   $��2��6O   $��Center=�6_   6|   �� 5&      -   5        $��cbDuty��5&      $��1��6)   $��3��66   $��30��6F   $��5q�6S   $��Center=�6^   6�   �� 5'      -   5        $��btnAdd��5'      $��添加��6'   $��1��64   $��4��6A   $��5q�6Q   $��30��6\   6   5(      5)      �� 5*   
   
�������$��1q�6)   $��5q�6=   $��2��6H   ._�5+      &   System.Windows.Controls.DataGrid  -   5        $��
dgUserInfo��5+         CanUserAddRows .     CanUserSortColumns .     AutoGenerateColumns .  $��14(�6w   $
��0,10q�6�      RowHeaderWidth$ 0Q�6�   ��.  	   Columns	 5,      4  .System.Windows.Controls.DataGridTemplateColumn  5-      ,  &System.Windows.Controls.DataGridColumn
   Header$
 序号��6-      DataGridHeaderStyle   HeaderStyle# [  69      Width5  /System.Windows.Controls.DataGridLengthConverter$ 50 6l      
IsReadOnly .    CellTemplate 5.      �� 5/      .R�50   "   �� $��Center=�6G   ��6(   ��  �� Path$ Index�6G   51      53      0  *System.Windows.Controls.DataGridTextColumn  54      $
 工号��6)   # [  66   $	 80* 6�    .  ! DataGridTextColumnCenterStyle1  +System.Windows.Controls.DataGridBoundColumn   ElementStyle# [ 6�      Binding 6i   �� $ UserNo�6�     55      $
 姓名��6)   # [  65   $	 80* 6�    . # [ 6�    6h   �� $ UserName�6�     56      $
 密码��6)   # [  65   $	 80* 6�    . # [ 6�    6h   �� $ Password�6�     57      $
 职务��6)   # [  65   $	 80* 6�    . # [ 6�    6h   �� $
 Duty�6�     58      $
 操作��6-   # [  69   $	 80* 6l    .  59      �� 5:      .R�5;   "   �  5<   &   �� 5=   *   $��* �6;   6G   �� 5>   *   $��* �6;   6G   .+�5@   &   �� $��btnAlter��6-   -	   5        $��修改��5@   =   $��Center=�6J   $��Center=�6h   $��0��6�   6�   �� 5A   &   $��	btnDelete��6-   -
   5        $��删除��5A   >   $��Center=�6K   $��Center=�6i   $��1��6�   6�   5B   #   5C      5E      5G      5H      5I      5J      